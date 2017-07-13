using System;
using System.Windows;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using GalaSoft.MvvmLight;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.View;

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    public sealed class TrayViewModel : ViewModelBase, ITrayViewModel
    {
        [NotNull]
        private readonly ILifetimeScope lifetimeScope;

        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly ISettingsRepository settingsRepository;

        public TrayViewModel([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
        {
            this.lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            this.settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ShowSettingsCommand = new CorrelationCommand(ShowSettings);
            ShowDictionaryCommand = new CorrelationCommand(ShowDictionary);
            ToggleActiveCommand = new CorrelationCommand(ToggleActive);
            ExitCommand = new CorrelationCommand(Exit);
            IsActive = settingsRepository.Get().IsActive;
        }

        #region Commands

        public ICommand ShowDictionaryCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region Command handlers

        private void ShowSettings()
        {
            logger.Info("Showing settings...");
            var dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            lifetimeScope.Resolve<WindowFactory<ISettingsWindow>>().GetOrCreateWindow(dictionaryWindowParameter).Restore();
        }

        private void ShowDictionary()
        {
            logger.Info("Showing dictionary...");
            var dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            if (dictionaryWindow == null)
            {
                var splashWindow = lifetimeScope.Resolve<ISplashScreenWindow>();
                splashWindow.Show();
                dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetOrCreateWindow();

                void LoadedHandler(object s, RoutedEventArgs e)
                {
                    dictionaryWindow.Loaded -= LoadedHandler;
                    splashWindow.Close();
                }

                dictionaryWindow.Loaded += LoadedHandler;
            }
            dictionaryWindow.Restore();
        }

        private void ToggleActive()
        {
            logger.Info("Toggling state...");
            IsActive = !IsActive;
            var settings = settingsRepository.Get();
            settings.IsActive = IsActive;
            settingsRepository.Save(settings);
            logger.Info($"New state is {IsActive}");
        }

        private void Exit()
        {
            logger.Info("Exitting application...");
            Application.Current.Shutdown();
        }

        #endregion

        #region Dependency Properties

        private bool isActive;

        public bool IsActive
        {
            get { return isActive; }
            [UsedImplicitly]
            set { Set(() => IsActive, ref isActive, value); }
        }

        #endregion
    }
}