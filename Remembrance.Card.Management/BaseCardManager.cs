using System;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.WPF;
using Scar.Common.WPF.Localization;

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

        protected BaseCardManager([NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));
            if (settingsRepository == null)
                throw new ArgumentNullException(nameof(settingsRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            Logger = logger;
            LifetimeScope = lifetimeScope;
            SettingsRepository = settingsRepository;
        }

        public void ShowCard(TranslationInfo translationInfo)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var window = TryCreateWindow(translationInfo);
                if (window == null)
                {
                    Logger.Debug($"No window to show for {translationInfo}");
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

        private static void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var window = (Window)sender;
            var r = SystemParameters.WorkArea;
            window.Left = r.Right - window.ActualWidth;
            window.Top = r.Bottom - window.ActualHeight;
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            var window = (Window)sender;
            window.Closed -= Window_Closed;
            window.SizeChanged -= Window_SizeChanged;
        }

        [CanBeNull]
        protected abstract IWindow TryCreateWindow([NotNull] TranslationInfo translationInfo);
    }
}