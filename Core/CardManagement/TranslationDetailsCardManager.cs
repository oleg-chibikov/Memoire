using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
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
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _windowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
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
