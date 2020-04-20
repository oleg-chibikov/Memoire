using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Remembrance.Contracts.CardManagement;
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
            ILog logger,
            SynchronizationContext synchronizationContext,
            IScopedWindowProvider scopedWindowProvider,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager) : base(logger, synchronizationContext, windowPositionAdjustmentManager)
        {
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
        }

        public async Task ShowCardAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow)
        {
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            Logger.TraceFormat("Creating window for {0}...", translationInfo);

            var translationDetailsCardWindow = await _scopedWindowProvider
                .GetScopedWindowAsync<ITranslationDetailsCardWindow, (IDisplayable?, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                .ConfigureAwait(false);
            SynchronizationContext.Send(x => WindowPositionAdjustmentManager.AdjustDetailsCardWindowPosition(translationDetailsCardWindow), null);
            ShowWindow(translationDetailsCardWindow);
            Logger.InfoFormat("Window for {0} has been opened", translationInfo);
        }
    }
}
