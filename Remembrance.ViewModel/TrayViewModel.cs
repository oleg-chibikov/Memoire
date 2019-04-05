using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.View.Settings;
using Remembrance.Resources;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Common.View.WindowFactory;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : BaseViewModel
    {
        private const string DateTimeFormat = @"HH\:mm\:ss";

        private const string TimeSpanFormat = @"hh\:mm\:ss";

        private readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;

        private readonly IApplicationTerminator _applicationTerminator;

        private readonly Func<ICardShowTimeProvider> _cardShowTimeProviderFactory;

        private readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        private readonly ILocalSettingsRepository _localSettingsRepository;

        private readonly ILog _logger;

        private readonly IMessageHub _messageHub;

        private readonly IPauseManager _pauseManager;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private readonly IWindowFactory<ISettingsWindow> _settingsWindowFactory;

        private readonly IWindowFactory<ISplashScreenWindow> _splashScreenWindowFactory;

        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        private readonly SynchronizationContext _synchronizationContext;

        private readonly Timer _timer;

        private ICardShowTimeProvider? _cardShowTimeProvider;

        private bool _isToolTipOpened;

        public TrayViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ILog logger,
            IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            IWindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            IWindowFactory<ISettingsWindow> settingsWindowFactory,
            IWindowFactory<ISplashScreenWindow> splashScreenWindowFactory,
            Func<ICardShowTimeProvider> cardShowTimeProviderFactory,
            IPauseManager pauseManager,
            IMessageHub messageHub,
            IRemembrancePathsProvider remembrancePathsProvider,
            ICommandManager commandManager,
            SynchronizationContext synchronizationContext,
            IApplicationTerminator applicationTerminator)
            : base(commandManager)
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
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _applicationTerminator = applicationTerminator ?? throw new ArgumentNullException(nameof(applicationTerminator));

            IsLoading = true;
            AddTranslationCommand = AddCommand(AddTranslationAsync);
            ShowSettingsCommand = AddCommand(ShowSettingsAsync);
            ShowDictionaryCommand = AddCommand(ShowDictionaryAsync);
            ToggleActiveCommand = AddCommand(ToggleActive);
            ToolTipOpenCommand = AddCommand(ToolTipOpenAsync);
            ToolTipCloseCommand = AddCommand(ToolTipClose);
            ExitCommand = AddCommand(Exit);
            OpenSharedFolderCommand = AddCommand(() => remembrancePathsProvider.OpenSharedFolder(_localSettingsRepository.SyncBus));
            OpenSettingsFolderCommand = AddCommand(remembrancePathsProvider.OpenSettingsFolder);
            ViewLogsCommand = AddCommand(remembrancePathsProvider.ViewLogs);
            IsActive = _localSettingsRepository.IsActive;
            _timer = new Timer(Timer_Tick, null, 0, 1000);

            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReason>(OnPauseReasonChanged));
        }

        public bool IsLoading { get; private set; }

        public ICommand AddTranslationCommand { get; }

        public string CardShowFrequency { get; private set; } = string.Empty;

        public string? CardVisiblePauseTime { get; private set; }

        public DateTime CurrentTime { get; private set; }

        public ICommand ExitCommand { get; }

        public bool IsActive { get; private set; }

        public bool IsPaused { get; private set; }

        public string? LastCardShowTime { get; private set; }

        public string NextCardShowTime { get; private set; } = string.Empty;

        public ICommand OpenSettingsFolderCommand { get; }

        public ICommand OpenSharedFolderCommand { get; }

        public string? PauseReasons { get; private set; }

        public ICommand ShowDictionaryCommand { get; }

        public ICommand ShowSettingsCommand { get; }

        public string TimeLeftToShowCard { get; private set; } = string.Empty;

        public ICommand ToggleActiveCommand { get; }

        public ICommand ToolTipCloseCommand { get; }

        public ICommand ToolTipOpenCommand { get; }

        public ICommand ViewLogsCommand { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                foreach (var subscriptionToken in _subscriptionTokens)
                {
                    _messageHub.Unsubscribe(subscriptionToken);
                }

                _subscriptionTokens.Clear();

                _timer.Dispose();
            }
        }

        private async Task AddTranslationAsync()
        {
            _logger.Trace("Showing Add Translation window...");
            await _addTranslationWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private void Exit()
        {
            _logger.Trace("Exiting application...");
            _applicationTerminator.Terminate();
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
            LastCardShowTime = provider.LastCardShowTime == null ? null : Texts.LastCardShowTime + ": " + provider.LastCardShowTime.Value.ToLocalTime().ToString(DateTimeFormat);
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

        private void Timer_Tick(object state)
        {
            if (!_isToolTipOpened)
            {
                return;
            }

            _synchronizationContext.Send(
                x =>
                {
                    // ReSharper disable once AssignmentIsFullyDiscarded
                    _ = SetTimesInfoAsync();
                },
                null);
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