//TODO: Set all bindings mode manually

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
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
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly WindowFactory<ISettingsWindow> _settingsWindowFactory;

        [NotNull]
        private readonly WindowFactory<ISplashScreenWindow> _splashScreenWindowFactory;

        [NotNull]
        private readonly DispatcherTimer _timer;

        private bool _isToolTipOpened;

        public TrayViewModel(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] WindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] WindowFactory<ISettingsWindow> settingsWindowFactory,
            [NotNull] WindowFactory<ISplashScreenWindow> splashScreenWindowFactory,
            [NotNull] IMessageHub messageHub,
            [NotNull] ICardShowTimeProvider cardShowTimeProvider)
        {
            _messageHub = messageHub;
            _cardShowTimeProvider = cardShowTimeProvider ?? throw new ArgumentNullException(nameof(cardShowTimeProvider));
            _splashScreenWindowFactory = splashScreenWindowFactory ?? throw new ArgumentNullException(nameof(splashScreenWindowFactory));
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            AddTranslationCommand = new AsyncCorrelationCommand(AddTranslationAsync);
            ShowSettingsCommand = new AsyncCorrelationCommand(ShowSettingsAsync);
            ShowDictionaryCommand = new AsyncCorrelationCommand(ShowDictionaryAsync);
            ToggleActiveCommand = new CorrelationCommand(ToggleActive);
            ToolTipOpenCommand = new CorrelationCommand(ToolTipOpen);
            ToolTipCloseCommand = new CorrelationCommand(ToolTipClose);
            ExitCommand = new CorrelationCommand(Exit);
            IsActive = localSettingsRepository.Get().IsActive;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            SetTimesInfo();
            _timer.Tick += Timer_Tick;
        }

        public bool IsActive { get; private set; }

        [NotNull]
        public string TimeLeftToShowCard { get; private set; }

        [CanBeNull]
        public string LastCardShowTime { get; private set; }

        [NotNull]
        public string NextCardShowTime { get; private set; }

        [NotNull]
        public string CardShowFrequency { get; private set; }

        public DateTime CurrentTime { get; private set; }

        [CanBeNull]
        public string PausedTime { get; private set; }

        [CanBeNull]
        public string PausedAt { get; private set; }

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
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isToolTipOpened)
            {
                return;
            }

            SetTimesInfo();
        }

        private void SetTimesInfo()
        {
            const string dateTimeFormat = @"HH\:mm\:ss";
            const string timeSpanFormat = @"hh\:mm\:ss";

            TimeLeftToShowCard = Texts.TimeToShow + ": " + _cardShowTimeProvider.TimeLeftToShowCard.ToString(timeSpanFormat);
            LastCardShowTime = _cardShowTimeProvider.LastCardShowTime == null
                ? null
                : Texts.LastCardShowTime + ": " + _cardShowTimeProvider.LastCardShowTime.Value.ToString(dateTimeFormat);
            NextCardShowTime = Texts.NextCardShowTime + ": " + _cardShowTimeProvider.NextCardShowTime.ToString(dateTimeFormat);
            CardShowFrequency = Texts.CardShowFrequency + ": " + _cardShowTimeProvider.CardShowFrequency.ToString(timeSpanFormat);
            PausedTime = _cardShowTimeProvider.PausedTime == TimeSpan.Zero
                ? null
                : Texts.PausedTime + ": " + _cardShowTimeProvider.PausedTime.ToString(timeSpanFormat);
            PausedAt = !_cardShowTimeProvider.IsPaused
                ? null
                : Texts.PausedAt + ": " + _cardShowTimeProvider.LastPausedTime.ToString(dateTimeFormat);
            CurrentTime = DateTime.Now;
        }

        private async Task AddTranslationAsync()
        {
            _logger.Trace("Showing Add Translation window...");
            await _addTranslationWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private void Exit()
        {
            _logger.Trace("Exitting application...");
            Application.Current.Shutdown();
        }

        private async Task ShowDictionaryAsync()
        {
            _logger.Trace("Showing dictionary...");
            await _dictionaryWindowFactory.ShowWindowAsync(_splashScreenWindowFactory, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task ShowSettingsAsync()
        {
            _logger.Trace("Showing settings...");
            await _settingsWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
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
            _logger.Trace("Toggling state...");
            IsActive = !IsActive;
            var settings = _localSettingsRepository.Get();
            settings.IsActive = IsActive;
            _localSettingsRepository.UpdateOrInsert(settings);
            _logger.InfoFormat("New state is {0}", IsActive);
            if (IsActive)
            {
                _logger.Trace("Resuming showing cards...");
                _messageHub.Publish(IntervalModificator.Resume);
            }
            else
            {
                _logger.Trace("Pausing showing cards...");
                _messageHub.Publish(IntervalModificator.Pause);
            }
        }
    }
}