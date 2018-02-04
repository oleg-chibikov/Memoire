using System;
using System.Threading;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal abstract class BaseCardManager
    {
        [NotNull]
        protected readonly ILifetimeScope LifetimeScope;

        [NotNull]
        protected readonly ILocalSettingsRepository LocalSettingsRepository;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        protected BaseCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ILocalSettingsRepository localSettingsRepository, [NotNull] ILog logger, [NotNull] SynchronizationContext synchronizationContext)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            LifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            LocalSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
        }

        public void ShowCard(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            _synchronizationContext.Send(
                x =>
                {
                    var window = TryCreateWindow(translationInfo, ownerWindow);
                    if (window == null)
                    {
                        Logger.Trace($"No window to show for {translationInfo}");
                        return;
                    }

                    CultureUtilities.ChangeCulture(LocalSettingsRepository.Get().UiLanguage);
                    window.Draggable = false;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                    if (window.AdvancedWindowStartupLocation == AdvancedWindowStartupLocation.Default)
                    {
                        window.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.BottomRight;
                    }

                    Logger.Info($"Showing {translationInfo}...");
                    window.ShowActivated = false;
                    window.Topmost = true;
                    window.Restore();
                }, null);
        }

        [CanBeNull]
        protected abstract IWindow TryCreateWindow([NotNull] TranslationInfo translationInfo, IWindow ownerWindow);
    }
}