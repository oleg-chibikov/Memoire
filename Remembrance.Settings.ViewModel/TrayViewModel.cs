using System;
using System.Windows;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.DAL.Contracts;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.View;

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : ITrayViewModel
    {
        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        public TrayViewModel([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ShowSettingsCommand = new CorrelationCommand(ShowSettings);
            ShowDictionaryCommand = new CorrelationCommand(ShowDictionary);
            ToggleActiveCommand = new CorrelationCommand(ToggleActive);
            ExitCommand = new CorrelationCommand(Exit);
            IsActive = settingsRepository.Get().IsActive;
        }

        #region Dependency Properties

        public bool IsActive { get; set; }

        #endregion

        #region Commands

        public ICommand ShowDictionaryCommand { get; }

        public ICommand ShowSettingsCommand { get; }

        public ICommand ToggleActiveCommand { get; }

        public ICommand ExitCommand { get; }

        #endregion

        #region Command handlers

        private void ShowSettings()
        {
            _logger.Info("Showing settings...");
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            _lifetimeScope.Resolve<WindowFactory<ISettingsWindow>>().GetOrCreateWindow(dictionaryWindowParameter).Restore();
        }

        private void ShowDictionary()
        {
            _logger.Info("Showing dictionary...");
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowIfExists();
            if (dictionaryWindow == null)
            {
                var splashWindow = _lifetimeScope.Resolve<ISplashScreenWindow>();
                splashWindow.Show();
                dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetOrCreateWindow();

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
            _logger.Info("Toggling state...");
            IsActive = !IsActive;
            var settings = _settingsRepository.Get();
            settings.IsActive = IsActive;
            _settingsRepository.Save(settings);
            _logger.Info($"New state is {IsActive}");
        }

        private void Exit()
        {
            _logger.Info("Exitting application...");
            Application.Current.Shutdown();
        }

        #endregion
    }
}