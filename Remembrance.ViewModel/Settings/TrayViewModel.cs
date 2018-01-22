//TODO: Set all bindings mode manually

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.View.Settings;
using Remembrance.Resources;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.View;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : IDisposable
    {
        [NotNull]
        private readonly WindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        [NotNull]
        private readonly ICardShowTimeProvider _cardShowTimeProvider;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly WindowFactory<ISettingsWindow> _settingsWindowFactory;

        [NotNull]
        private readonly WindowFactory<ISplashScreenWindow> _splashScreenWindowFactory;

        [NotNull]
        private readonly DispatcherTimer _timer;

        private bool _isToolTipOpened;

        public TrayViewModel(
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] WindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] WindowFactory<ISettingsWindow> settingsWindowFactory,
            [NotNull] WindowFactory<ISplashScreenWindow> splashScreenWindowFactory,
            [NotNull] IMessageHub messenger,
            [NotNull] ICardShowTimeProvider cardShowTimeProvider)
        {
            _messenger = messenger;
            _cardShowTimeProvider = cardShowTimeProvider ?? throw new ArgumentNullException(nameof(cardShowTimeProvider));
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
            ToolTipOpenCommand = new CorrelationCommand(ToolTipOpen);
            ToolTipCloseCommand = new CorrelationCommand(ToolTipClose);
            ExitCommand = new CorrelationCommand(Exit);
            IsActive = settingsRepository.Get()
                .IsActive;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;
        }

        public bool IsActive { get; private set; }

        [NotNull]
        public string CardShowTimeInfo { get; private set; }

        [CanBeNull]
        public string CardShowPauseInfo { get; private set; }

        [NotNull]
        public ICommand AddTranslationCommand { get; }

        [NotNull]
        public ICommand ShowDictionaryCommand { get; }

        [NotNull]
        public ICommand ToolTipCloseCommand { get; }

        [NotNull]
        public ICommand ToolTipOpenCommand { get; }

        [NotNull]
        public ICommand ShowSettingsCommand { get; }

        [NotNull]
        public ICommand ToggleActiveCommand { get; }

        [NotNull]
        public ICommand ExitCommand { get; }

        public void Dispose()
        {
            _messenger.Dispose();
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isToolTipOpened)
            {
                return;
            }

            const string format = @"hh\:mm\:ss";
            CardShowTimeInfo = string.Format(
                Texts.CardShowTimeInfo,
                _cardShowTimeProvider.TimeLeftToShowCard.ToString(format),
                (_cardShowTimeProvider.LastCardShowTime ?? DateTime.MinValue).ToString(format),
                _cardShowTimeProvider.CardShowFrequency.ToString(format),
                _cardShowTimeProvider.PausedTime > TimeSpan.Zero
                    ? string.Format(Texts.CardShowPauseTimeInfo, _cardShowTimeProvider.PausedTime.ToString(format))
                    : null,
                DateTime.Now.ToString(format));
            CardShowPauseInfo = _cardShowTimeProvider.IsPaused
                ? string.Format(Texts.CardShowIsPausedInfo, _cardShowTimeProvider.LastPausedTime.ToString(format))
                : null;
        }

        private void AddTranslation()
        {
            _logger.Info("Showing Add Translation window...");
            _addTranslationWindowFactory.ShowWindow();
        }

        private void Exit()
        {
            _logger.Info("Exitting application...");
            Application.Current.Shutdown();
        }

        private void ShowDictionary()
        {
            _logger.Info("Showing dictionary...");
            _dictionaryWindowFactory.ShowWindow(_splashScreenWindowFactory);
        }

        private void ShowSettings()
        {
            _logger.Info("Showing settings...");
            _settingsWindowFactory.ShowWindow();
        }

        private void ToolTipClose()
        {
            _isToolTipOpened = false;
        }

        private void ToolTipOpen()
        {
            _isToolTipOpened = true;
        }

        private void ToggleActive()
        {
            _logger.Info("Toggling state...");
            IsActive = !IsActive;
            var settings = _settingsRepository.Get();
            settings.IsActive = IsActive;
            _settingsRepository.UpdateOrInsert(settings);
            _logger.Info($"New state is {IsActive}");
            if (IsActive)
            {
                _logger.Trace("Resuming showing cards...");
                _messenger.Publish(IntervalModificator.Resume);
            }
            else
            {
                _logger.Trace("Pausing showing cards...");
                _messenger.Publish(IntervalModificator.Pause);
            }
        }
    }
}