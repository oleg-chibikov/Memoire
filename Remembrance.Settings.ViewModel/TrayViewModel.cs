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
using Scar.Common.WPF.View.Contracts;

//TODO: Set all bindings mode manually

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : ITrayViewModel
    {
        [NotNull]
        private readonly object _windowLocker = new object();

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly WindowFactory<ISettingsWindow> _settingsWindowFactory;

        [NotNull]
        private readonly WindowFactory<ISplashScreenWindow> _splashScreenWindowFactory;

        public TrayViewModel(
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] WindowFactory<ISettingsWindow> settingsWindowFactory,
            [NotNull] WindowFactory<ISplashScreenWindow> splashScreenWindowFactory)
        {
            _splashScreenWindowFactory = splashScreenWindowFactory ?? throw new ArgumentNullException(nameof(splashScreenWindowFactory));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ShowSettingsCommand = new CorrelationCommand(ShowSettings);
            ShowDictionaryCommand = new CorrelationCommand(ShowDictionary);
            ToggleActiveCommand = new CorrelationCommand(ToggleActive);
            ExitCommand = new CorrelationCommand(Exit);
            IsActive = settingsRepository.Get().IsActive;
        }

        #region Dependency Properties

        public bool IsActive { get; private set; }

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
            lock (_windowLocker)
            {
                var dictionaryWindow = _dictionaryWindowFactory.GetWindowIfExists();
                var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
                _settingsWindowFactory.GetOrCreateWindow(dictionaryWindowParameter).Restore();
            }
        }

        private void ShowDictionary()
        {
            _logger.Info("Showing dictionary...");
            lock (_windowLocker)
            {
                var dictionaryWindow = _dictionaryWindowFactory.GetWindowIfExists();
                ShowWithSplash(dictionaryWindow, _dictionaryWindowFactory);
            }
        }

        private void ShowWithSplash<TWindow>([CanBeNull] TWindow window, [NotNull] WindowFactory<TWindow> windowFactory)
            where TWindow : class, IWindow
        {
            if (window == null)
            {
                window = windowFactory.GetOrCreateWindow();
                var splashScreenWindow = _splashScreenWindowFactory.GetOrCreateWindow();

                splashScreenWindow.Show();

                void LoadedHandler(object s, RoutedEventArgs e)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    window.Loaded -= LoadedHandler;
                    splashScreenWindow.Close();
                }

                window.Loaded += LoadedHandler;
            }
            window.Restore();
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