using System;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View.Card;
using Microsoft.Extensions.Logging;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;

namespace Mémoire.Core.CardManagement
{
    sealed class TranslationDetailsCardManager : ITranslationDetailsCardManager
    {
        readonly IScopedWindowProvider _scopedWindowProvider;
        readonly ILogger _logger;
        readonly SynchronizationContext _synchronizationContext;
        readonly IWindowPositionAdjustmentManager _windowPositionAdjustmentManager;

        public TranslationDetailsCardManager(
            ILogger<TranslationDetailsCardManager> logger,
            SynchronizationContext synchronizationContext,
            IScopedWindowProvider scopedWindowProvider,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _windowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        public async Task ShowCardAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow)
        {
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _logger.LogTrace("Creating window for {0}...", translationInfo);

            var window = await _scopedWindowProvider.GetScopedWindowAsync<ITranslationDetailsCardWindow, (IDisplayable?, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                .ConfigureAwait(false);
            _synchronizationContext.Send(
                x =>
                {
                    _windowPositionAdjustmentManager.AdjustDetailsCardWindowPosition(window);
                    window.Restore();
                },
                null);
            _logger.LogInformation("Window for {0} has been opened", translationInfo);
        }
    }
}
