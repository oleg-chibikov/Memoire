using System;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

// ReSharper disable MemberCanBePrivate.Global

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal abstract class BaseCardManager
    {
        [NotNull]
        protected readonly ILifetimeScope LifetimeScope;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly ISettingsRepository SettingsRepository;

        protected BaseCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            SettingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        }

        public void ShowCard(TranslationInfo translationInfo)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    var window = TryCreateWindow(translationInfo);
                    if (window == null)
                    {
                        Logger.Trace($"No window to show for {translationInfo}");
                        return;
                    }

                    window.CanDrag = false;
                    Logger.Info($"Showing {translationInfo}");
                    CultureUtilities.ChangeCulture(SettingsRepository.Get().UiLanguage);
                    window.SizeChanged += Window_SizeChanged;
                    window.Closed += Window_Closed;
                    window.Topmost = true;
                    window.Show();
                });
        }

        [CanBeNull]
        protected abstract IWindow TryCreateWindow([NotNull] TranslationInfo translationInfo);

        private static void Window_Closed(object sender, EventArgs e)
        {
            var window = (Window) sender;
            window.Closed -= Window_Closed;
            window.SizeChanged -= Window_SizeChanged;
        }

        private static void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var window = (Window) sender;
            var r = SystemParameters.WorkArea;
            window.Left = r.Right - window.ActualWidth;
            window.Top = r.Bottom - window.ActualHeight;
        }
    }
}