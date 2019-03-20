using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.View.Settings;
using Remembrance.Resources;
using Scar.Common.View.WindowFactory;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : IDisposable
    {
        private const string DateTimeFormat = @"HH\:mm\:ss";

        private const string TimeSpanFormat = @"hh\:mm\:ss";

        [NotNull]
        private readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        [NotNull]
        private readonly Func<ICardShowTimeProvider> _cardShowTimeProviderFactory;

        [NotNull]
        private readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        [NotNull]
        private readonly IWindowFactory<ISettingsWindow> _settingsWindowFactory;

        [NotNull]
        private readonly IWindowFactory<ISplashScreenWindow> _splashScreenWindowFactory;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly DispatcherTimer _timer;

        [CanBeNull]
        private ICardShowTimeProvider _cardShowTimeProvider;

        [NotNull]
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private bool _isToolTipOpened;

        public TrayViewModel(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            [NotNull] IWindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] IWindowFactory<ISettingsWindow> settingsWindowFactory,
            [NotNull] IWindowFactory<ISplashScreenWindow> splashScreenWindowFactory,
            [NotNull] Func<ICardShowTimeProvider> cardShowTimeProviderFactory,
            [NotNull] IPauseManager pauseManager,
            [NotNull] IMessageHub messageHub,
            [NotNull] IRemembrancePathsProvider remembrancePathsProvider)
        {
            _cardShowTimeProviderFactory = cardShowTimeProviderFactory ?? throw new ArgumentNullException(nameof(cardShowTimeProviderFactory));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _splashScreenWindowFactory = splashScreenWindowFactory ?? throw new ArgumentNullException(nameof(splashScreenWindowFactory));
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = remembrancePathsProvider ?? throw new ArgumentNullException(nameof(remembrancePathsProvider));

            IsLoading = true;
            AddTranslationCommand = new AsyncCorrelationCommand(AddTranslationAsync);
            ShowSettingsCommand = new AsyncCorrelationCommand(ShowSettingsAsync);
            ShowDictionaryCommand = new AsyncCorrelationCommand(ShowDictionaryAsync);
            ToggleActiveCommand = new CorrelationCommand(ToggleActive);
            ToolTipOpenCommand = new AsyncCorrelationCommand(ToolTipOpenAsync);
            ToolTipCloseCommand = new CorrelationCommand(ToolTipClose);
            ExitCommand = new CorrelationCommand(Exit);
            OpenSharedFolderCommand = new CorrelationCommand(() => remembrancePathsProvider.OpenSharedFolder(_localSettingsRepository.SyncBus));
            OpenSettingsFolderCommand = new CorrelationCommand(remembrancePathsProvider.OpenSettingsFolder);
            ViewLogsCommand = new CorrelationCommand(remembrancePathsProvider.ViewLogs);
            IsActive = _localSettingsRepository.IsActive;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;

            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReason>(OnPauseReasonChanged));
        }

        public bool IsLoading { get; private set; }

        [NotNull]
        public ICommand AddTranslationCommand { get; }

        [NotNull]
        public string CardShowFrequency { get; private set; } = string.Empty;

        [CanBeNull]
        public string CardVisiblePauseTime { get; private set; }

        public DateTime CurrentTime { get; private set; }

        [NotNull]
        public ICommand ExitCommand { get; }

        public bool IsActive { get; private set; }

        public bool IsPaused { get; private set; }

        [CanBeNull]
        public string LastCardShowTime { get; private set; }

        [NotNull]
        public string NextCardShowTime { get; private set; } = string.Empty;

        [NotNull]
        public ICommand OpenSettingsFolderCommand { get; }

        [NotNull]
        public ICommand OpenSharedFolderCommand { get; }

        [CanBeNull]
        public string PauseReasons { get; private set; }

        [NotNull]
        public ICommand ShowDictionaryCommand { get; }

        [NotNull]
        public ICommand ShowSettingsCommand { get; }

        [NotNull]
        public string TimeLeftToShowCard { get; private set; } = string.Empty;

        [NotNull]
        public ICommand ToggleActiveCommand { get; }

        [NotNull]
        public ICommand ToolTipCloseCommand { get; }

        [NotNull]
        public ICommand ToolTipOpenCommand { get; }

        [NotNull]
        public ICommand ViewLogsCommand { get; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messageHub.Unsubscribe(subscriptionToken);
            }

            _subscriptionTokens.Clear();

            _timer.Tick -= Timer_Tick;
            _timer.Stop();
        }

        [NotNull]
        private async Task AddTranslationAsync()
        {
            _logger.Trace("Showing Add Translation window...");
            await _addTranslationWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private void Exit()
        {
            _logger.Trace("Exiting application...");
            Application.Current.Shutdown();
        }

        private void OnPauseReasonChanged(PauseReason reason)
        {
            IsPaused = _pauseManager.IsPaused;
        }

        private async Task SetTimesInfoAsync()
        {
            CurrentTime = DateTime.Now;
            PauseReasons = _pauseManager.GetPauseReasons();

            await _semaphore.WaitAsync();
            var provider = _cardShowTimeProvider ?? await SetupCardShowTimeProviderAsync();
            _semaphore.Release();

            TimeLeftToShowCard = Texts.TimeToShow + ": " + provider.TimeLeftToShowCard.ToString(TimeSpanFormat);
            LastCardShowTime = provider.LastCardShowTime == null
                ? null
                : Texts.LastCardShowTime + ": " + provider.LastCardShowTime.Value.ToLocalTime().ToString(DateTimeFormat);
            NextCardShowTime = Texts.NextCardShowTime + ": " + provider.NextCardShowTime.ToLocalTime().ToString(DateTimeFormat);
            CardShowFrequency = Texts.CardShowFrequency + ": " + provider.CardShowFrequency.ToString(TimeSpanFormat);
            var cardVisiblePauseTime = _pauseManager.GetPauseInfo(PauseReason.CardIsVisible).GetPauseTime();
            CardVisiblePauseTime = cardVisiblePauseTime == TimeSpan.Zero ? null : Texts.CardVisiblePauseTime + ": " + cardVisiblePauseTime.ToString(TimeSpanFormat);
        }

        private async Task<ICardShowTimeProvider> SetupCardShowTimeProviderAsync()
        {
            var provider = _cardShowTimeProvider = await Task.Run(() => _cardShowTimeProviderFactory());
            IsLoading = false;
            return provider;
        }

        [NotNull]
        private async Task ShowDictionaryAsync()
        {
            _logger.Trace("Showing dictionary...");
            await _dictionaryWindowFactory.ShowWindowAsync(_splashScreenWindowFactory, CancellationToken.None).ConfigureAwait(false);
        }

        [NotNull]
        private async Task ShowSettingsAsync()
        {
            _logger.Trace("Showing settings...");
            await _settingsWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isToolTipOpened)
            {
                return;
            }

            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = SetTimesInfoAsync();
        }

        private void ToggleActive()
        {
            _logger.Trace("Toggling state...");
            IsActive = !IsActive;
            _localSettingsRepository.IsActive = IsActive;
            _logger.InfoFormat("New state is {0}", IsActive);
            if (IsActive)
            {
                _pauseManager.Resume(PauseReason.InactiveMode);
            }
            else
            {
                _pauseManager.Pause(PauseReason.InactiveMode);
            }
        }

        private void ToolTipClose()
        {
            _isToolTipOpened = false;
        }

        private async Task ToolTipOpenAsync()
        {
            await SetTimesInfoAsync();
            _isToolTipOpened = true;
        }
    }
}