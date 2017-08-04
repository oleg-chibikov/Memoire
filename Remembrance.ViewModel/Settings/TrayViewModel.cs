using System;
using System.Windows;
using System.Windows.Input;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.View.Settings;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.View;

//TODO: Set all bindings mode manually

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel
    {
        [NotNull]
        private readonly WindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

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
            [NotNull] WindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] WindowFactory<ISettingsWindow> settingsWindowFactory,
            [NotNull] WindowFactory<ISplashScreenWindow> splashScreenWindowFactory)
        {
            _splashScreenWindowFactory = splashScreenWindowFactory ?? throw new ArgumentNullException(nameof(splashScreenWindowFactory));
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AddTranslationCommand = new CorrelationCommand(AddTranslation);
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

        public ICommand AddTranslationCommand { get; }

        public ICommand ShowDictionaryCommand { get; }

        public ICommand ShowSettingsCommand { get; }

        public ICommand ToggleActiveCommand { get; }

        public ICommand ExitCommand { get; }

        #endregion

        #region Command handlers

        private void AddTranslation()
        {
            _logger.Info("Showing Add Translation window...");
            _addTranslationWindowFactory.ShowWindow();
        }

        private void ShowSettings()
        {
            _logger.Info("Showing settings...");
            _settingsWindowFactory.ShowWindow();
        }

        private void ShowDictionary()
        {
            _logger.Info("Showing dictionary...");
            _dictionaryWindowFactory.ShowWindow(_splashScreenWindowFactory);
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