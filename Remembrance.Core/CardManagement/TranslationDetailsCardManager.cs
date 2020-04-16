using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    sealed class TranslationDetailsCardManager : BaseCardManager, ITranslationDetailsCardManager
    {
        readonly IScopedWindowProvider _scopedWindowProvider;

        public TranslationDetailsCardManager(
            ILocalSettingsRepository localSettingsRepository,
            ILog logger,
            SynchronizationContext synchronizationContext,
            IScopedWindowProvider scopedWindowProvider,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
            : base(localSettingsRepository, logger, synchronizationContext, windowPositionAdjustmentManager)
        {
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
        }

        protected override async Task<IDisplayable?> TryCreateWindowAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow)
        {
            Logger.TraceFormat("Creating window for {0}...", translationInfo);

            var translationDetailsCardWindow = await _scopedWindowProvider
                .GetScopedWindowAsync<ITranslationDetailsCardWindow, (IDisplayable?, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                .ConfigureAwait(false);
            SynchronizationContext.Send(x => WindowPositionAdjustmentManager.AdjustDetailsCardWindowPosition(translationDetailsCardWindow), null);
            return translationDetailsCardWindow;
        }
    }
}
