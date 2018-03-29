using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

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

        protected BaseCardManager([NotNull] ILocalSettingsRepository localSettingsRepository, [NotNull] ILog logger, [NotNull] SynchronizationContext synchronizationContext)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SynchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            LocalSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
        }

        [NotNull]
        public async Task ShowCardAsync([NotNull] TranslationInfo translationInfo, [CanBeNull] IWindow ownerWindow)
        {
            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            var window = await TryCreateWindowAsync(translationInfo, ownerWindow).ConfigureAwait(false);
            if (window == null)
            {
                Logger.DebugFormat("No window to show for {0}", translationInfo);
                return;
            }

            CultureUtilities.ChangeCulture(LocalSettingsRepository.UiLanguage);
            SynchronizationContext.Send(
                x =>
                {
                    window.Draggable = false;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    if (window.AdvancedWindowStartupLocation == AdvancedWindowStartupLocation.Default)
                    {
                        window.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.BottomRight;
                    }

                    window.ShowActivated = false;
                    window.Topmost = true;
                    window.Restore();
                },
                null);
            Logger.InfoFormat("Window for {0} has been opened", translationInfo);
        }

        [NotNull]
        [ItemCanBeNull]
        protected abstract Task<IWindow> TryCreateWindowAsync([NotNull] TranslationInfo translationInfo, IWindow ownerWindow);
    }
}