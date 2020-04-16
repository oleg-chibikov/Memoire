using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    abstract class BaseCardManager
    {
        protected BaseCardManager(
            ILocalSettingsRepository localSettingsRepository,
            ILog logger,
            SynchronizationContext synchronizationContext,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SynchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            WindowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
            LocalSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
        }

        protected ILocalSettingsRepository LocalSettingsRepository { get; }

        protected ILog Logger { get; }

        protected SynchronizationContext SynchronizationContext { get; }

        protected IWindowPositionAdjustmentManager WindowPositionAdjustmentManager { get; }

        public async Task ShowCardAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow)
        {
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            var window = await TryCreateWindowAsync(translationInfo, ownerWindow).ConfigureAwait(false);
            if (window == null)
            {
                Logger.DebugFormat("No window to show for {0}", translationInfo);
                return;
            }

            // CultureUtilities.ChangeCulture(LocalSettingsRepository.UiLanguage);
            SynchronizationContext.Send(
                x =>
                {
                    WindowPositionAdjustmentManager.AdjustAnyWindowPosition(window);
                    window.Restore();
                },
                null);
            Logger.InfoFormat("Window for {0} has been opened", translationInfo);
        }

        protected abstract Task<IDisplayable?> TryCreateWindowAsync(TranslationInfo translationInfo, IDisplayable? ownerWindow);
    }
}
