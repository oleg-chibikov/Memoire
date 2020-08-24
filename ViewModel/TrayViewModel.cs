using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.View.Settings;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Common.View.WindowCreation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TrayViewModel : BaseViewModel
    {
        const string DateTimeFormat = @"HH\:mm\:ss";
        const string TimeSpanFormat = @"hh\:mm\:ss";
        readonly IWindowFactory<IAddTranslationWindow> _addTranslationWindowFactory;
        readonly IApplicationTerminator _applicationTerminator;
        readonly Func<ICardShowTimeProvider> _cardShowTimeProviderFactory;
        readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly IPauseManager _pauseManager;
        readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        readonly IWindowFactory<ISettingsWindow> _settingsWindowFactory;
        readonly IWindowDisplayer _windowDisplayer;
        readonly Func<ILoadingWindow> _loadingWindowFactory;
        readonly IList<Guid> _subscriptionTokens = new List<Guid>();
        readonly SynchronizationContext _synchronizationContext;
        readonly Timer _timer;
        ICardShowTimeProvider? _cardShowTimeProvider;
        bool _isToolTipOpened;

        public TrayViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ILogger<TrayViewModel> logger,
            IWindowFactory<IAddTranslationWindow> addTranslationWindowFactory,
            IWindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            IWindowFactory<ISettingsWindow> settingsWindowFactory,
            IWindowDisplayer windowDisplayer,
            Func<ICardShowTimeProvider> cardShowTimeProviderFactory,
            IPauseManager pauseManager,
            IMessageHub messageHub,
            IPathsProvider pathsProvider,
            ICommandManager commandManager,
            SynchronizationContext synchronizationContext,
            IApplicationTerminator applicationTerminator,
            Func<ILoadingWindow> loadingWindowFactory) : base(commandManager)
        {
            _cardShowTimeProviderFactory = cardShowTimeProviderFactory ?? throw new ArgumentNullException(nameof(cardShowTimeProviderFactory));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _windowDisplayer = windowDisplayer ?? throw new ArgumentNullException(nameof(windowDisplayer));
            _addTranslationWindowFactory = addTranslationWindowFactory ?? throw new ArgumentNullException(nameof(addTranslationWindowFactory));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = pathsProvider ?? throw new ArgumentNullException(nameof(pathsProvider));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _applicationTerminator = applicationTerminator ?? throw new ArgumentNullException(nameof(applicationTerminator));
            _loadingWindowFactory = loadingWindowFactory ?? throw new ArgumentNullException(nameof(loadingWindowFactory));

            IsLoading = true;
            AddTranslationCommand = AddCommand(AddTranslationAsync);
            ShowSettingsCommand = AddCommand(ShowSettingsAsync);
            ShowDictionaryCommand = AddCommand(ShowDictionaryAsync);
            ToggleActiveCommand = AddCommand(ToggleActive);
            ToolTipOpenCommand = AddCommand(ToolTipOpenAsync);
            ToolTipCloseCommand = AddCommand(ToolTipClose);
            ExitCommand = AddCommand(Exit);
            OpenSharedFolderCommand = AddCommand(() => pathsProvider.OpenSharedFolder(_localSettingsRepository.SyncEngine));
            OpenSettingsFolderCommand = AddCommand(pathsProvider.OpenSettingsFolder);
            ViewLogsCommand = AddCommand(pathsProvider.ViewLogs);
            IsActive = _localSettingsRepository.IsActive;
            _timer = new Timer(Timer_Tick, null, 0, 1000);

            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReasons>(HandlePauseReasonChanged));
        }

        public bool IsLoading { get; private set; }

        public ICommand AddTranslationCommand { get; }

        public string CardShowFrequency { get; private set; } = string.Empty;

        public string? CardVisiblePauseTime { get; private set; }

        public DateTimeOffset CurrentTime { get; private set; }

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
                _semaphore.Dispose();
            }
        }

        async Task AddTranslationAsync()
        {
            _logger.LogTrace("Showing Add Translation window...");
            await _addTranslationWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        void Exit()
        {
            _logger.LogTrace("Exiting application...");
            _applicationTerminator.Terminate();
        }

        void HandlePauseReasonChanged(PauseReasons reasons)
        {
            IsPaused = _pauseManager.IsPaused;
        }

        async Task SetTimesInfoAsync()
        {
            CurrentTime = DateTimeOffset.Now;
            PauseReasons = _pauseManager.GetPauseReasons();

            await _semaphore.WaitAsync().ConfigureAwait(false);
            var provider = _cardShowTimeProvider ?? await SetupCardShowTimeProviderAsync().ConfigureAwait(false);
            _semaphore.Release();

            TimeLeftToShowCard = Texts.TimeToShow + ": " + provider.TimeLeftToShowCard.ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
            LastCardShowTime = provider.LastCardShowTime == null
                ? null
                : Texts.LastCardShowTime + ": " + provider.LastCardShowTime.Value.ToLocalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            NextCardShowTime = Texts.NextCardShowTime + ": " + provider.NextCardShowTime.ToLocalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);
            CardShowFrequency = Texts.CardShowFrequency + ": " + provider.CardShowFrequency.ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
            var cardVisiblePauseTime = _pauseManager.GetPauseInfo(Contracts.DAL.Model.PauseReasons.CardIsVisible).GetPauseTime();
            CardVisiblePauseTime = cardVisiblePauseTime == TimeSpan.Zero ? null : Texts.CardVisiblePauseTime + ": " + cardVisiblePauseTime.ToString(TimeSpanFormat, CultureInfo.InvariantCulture);
        }

        async Task<ICardShowTimeProvider> SetupCardShowTimeProviderAsync()
        {
            var provider = _cardShowTimeProvider = await Task.Run(() => _cardShowTimeProviderFactory()).ConfigureAwait(false);
            IsLoading = false;
            return provider;
        }

        async Task ShowDictionaryAsync()
        {
            _logger.LogTrace("Showing dictionary...");
            if (await _dictionaryWindowFactory.GetWindowIfExistsAsync(CancellationToken.None).ConfigureAwait(false) != null)
            {
                return;
            }

            var executeInLoadingWindowContext = _windowDisplayer.DisplayWindow(_loadingWindowFactory);
            var executeInDictionaryWindowContext = await _dictionaryWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);

            executeInDictionaryWindowContext(
                dictionaryWindow =>
                {
                    void DictionaryWindowContentRendered(object sender, EventArgs e)
                    {
                        if (dictionaryWindow != null)
                        {
                            dictionaryWindow.ContentRendered -= DictionaryWindowContentRendered;
                        }

                        executeInLoadingWindowContext(
                            loadingWindow =>
                            {
                                loadingWindow.Close();
                            });
                    }

                    dictionaryWindow.ContentRendered += DictionaryWindowContentRendered;
                });
        }

        async Task ShowSettingsAsync()
        {
            _logger.LogTrace("Showing settings...");
            await _settingsWindowFactory.ShowWindowAsync(CancellationToken.None).ConfigureAwait(false);
        }

        void Timer_Tick(object state)
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

        void ToggleActive()
        {
            _logger.LogTrace("Toggling state...");
            IsActive = !IsActive;
            _localSettingsRepository.IsActive = IsActive;
            _logger.LogInformation("New state is {0}", IsActive);
            if (IsActive)
            {
                _pauseManager.ResumeActivity(Contracts.DAL.Model.PauseReasons.InactiveMode);
            }
            else
            {
                _pauseManager.PauseActivity(Contracts.DAL.Model.PauseReasons.InactiveMode);
            }
        }

        void ToolTipClose()
        {
            _isToolTipOpened = false;
        }

        async Task ToolTipOpenAsync()
        {
            await SetTimesInfoAsync().ConfigureAwait(false);
            _isToolTipOpened = true;
        }
    }
}
