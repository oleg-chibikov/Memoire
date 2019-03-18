using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal abstract class BaseCardManager
    {
        [NotNull]
        protected readonly ILocalSettingsRepository LocalSettingsRepository;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly SynchronizationContext SynchronizationContext;

        [NotNull]
        protected readonly IWindowPositionAdjustmentManager WindowPositionAdjustmentManager;

        protected BaseCardManager(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SynchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            WindowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
            LocalSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
        }

        [NotNull]
        public async Task ShowCardAsync([NotNull] TranslationInfo translationInfo, [CanBeNull] IDisplayable ownerWindow)
        {
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            var window = await TryCreateWindowAsync(translationInfo, ownerWindow).ConfigureAwait(false);
            if (window == null)
            {
                Logger.DebugFormat("No window to show for {0}", translationInfo);
                return;
            }

            //CultureUtilities.ChangeCulture(LocalSettingsRepository.UiLanguage);
            SynchronizationContext.Send(
                x =>
                {
                    WindowPositionAdjustmentManager.AdjustAnyWindowPosition(window);
                    window.Restore();
                },
                null);
            Logger.InfoFormat("Window for {0} has been opened", translationInfo);
        }

        [NotNull]
        [ItemCanBeNull]
        protected abstract Task<IDisplayable> TryCreateWindowAsync([NotNull] TranslationInfo translationInfo, IDisplayable ownerWindow);
    }
}