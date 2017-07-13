using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;
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
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            ShowDictionaryCommand = new RelayCommand(ShowDictionary);
            ToggleActiveCommand = new RelayCommand(ToggleActive);
            ExitCommand = new RelayCommand(Exit);
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
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            logger.Info("Showing settings...");
            var dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            lifetimeScope.Resolve<WindowFactory<ISettingsWindow>>().GetOrCreateWindow(dictionaryWindowParameter).Restore();
        }

        private void ShowDictionary()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            logger.Info("Showing dictionary...");
            var dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            if (dictionaryWindow == null)
            {
                var splashWindow = lifetimeScope.Resolve<ISplashScreenWindow>();
                splashWindow.Show();
                dictionaryWindow = lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetOrCreateWindow();
                RoutedEventHandler loadedHandler = null;
                loadedHandler = (s, e) =>
                {
                    dictionaryWindow.Loaded -= loadedHandler;
                    splashWindow.Close();
                };
                dictionaryWindow.Loaded += loadedHandler;
            }
            dictionaryWindow.Restore();
        }

        private void ToggleActive()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            logger.Info("Toggling state...");
            IsActive = !IsActive;
            var settings = settingsRepository.Get();
            settings.IsActive = IsActive;
            settingsRepository.Save(settings);
            logger.Info($"New state is {IsActive}");
        }

        private void Exit()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
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