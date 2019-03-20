using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowFactory;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsCardManager : BaseCardManager, ITranslationDetailsCardManager
    {
        [NotNull]
        private readonly IScopedWindowProvider _scopedWindowProvider;

        public TranslationDetailsCardManager(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IScopedWindowProvider scopedWindowProvider,
            [NotNull] IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
            : base(localSettingsRepository, logger, synchronizationContext, windowPositionAdjustmentManager)
        {
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
        }

        [ItemNotNull]
        protected override async Task<IDisplayable?> TryCreateWindowAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow)
        {
            Logger.TraceFormat("Creating window for {0}...", translationInfo);

            var translationDetailsCardWindow = await _scopedWindowProvider
                .GetScopedWindowAsync<ITranslationDetailsCardWindow, (IDisplayable?, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                .ConfigureAwait(false);
            SynchronizationContext.Send(
                x => WindowPositionAdjustmentManager.AdjustDetailsCardWindowPosition(translationDetailsCardWindow),
                null);
            return translationDetailsCardWindow;
        }
    }
}