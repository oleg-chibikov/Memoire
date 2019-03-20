using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shell;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Exchange;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Languages.Data;
using Remembrance.Contracts.Sync;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;
using Scar.Common.Events;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class SettingsViewModel : IRequestCloseViewModel, IDisposable
    {
        [NotNull]
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        [NotNull]
        private readonly ICardsExchanger _cardsExchanger;

        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly IRemembrancePathsProvider _remembrancePathsProvider;

        private bool _saved;

        private string _uiLanguage;

        public SettingsViewModel(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messageHub,
            [NotNull] ICardsExchanger cardsExchanger,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IPauseManager pauseManager,
            [NotNull] ProcessBlacklistViewModel processBlacklistViewModel,
            [NotNull] ILanguageManager languageManager,
            [NotNull] IRemembrancePathsProvider remembrancePathsProvider)
        {
            _ = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _cardsExchanger = cardsExchanger ?? throw new ArgumentNullException(nameof(cardsExchanger));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            ProcessBlacklistViewModel = processBlacklistViewModel ?? throw new ArgumentNullException(nameof(processBlacklistViewModel));
            _remembrancePathsProvider = remembrancePathsProvider ?? throw new ArgumentNullException(nameof(remembrancePathsProvider));

            var blacklistedProcesses = _localSettingsRepository.BlacklistedProcesses;
            if (blacklistedProcesses != null)
            {
                foreach (var processInfo in blacklistedProcesses)
                {
                    ProcessBlacklistViewModel.BlacklistedProcesses.Add(processInfo);
                }
            }

            IList<SyncBus> syncBuses = new List<SyncBus>
            {
                SyncBus.NoSync
            };
            if (_remembrancePathsProvider.DropBoxPath != null)
            {
                syncBuses.Add(SyncBus.Dropbox);
            }

            if (_remembrancePathsProvider.OneDrivePath != null)
            {
                syncBuses.Add(SyncBus.OneDrive);
            }

            SyncBuses = syncBuses.ToArray();
            AvailableTranslationLanguages = languageManager.GetAvailableSourceLanguages(false);
            SelectedPreferredLanguage = _settingsRepository.PreferredLanguage;
            TtsSpeaker = _settingsRepository.TtsSpeaker;
            UiLanguage = _localSettingsRepository.UiLanguage;
            TtsVoiceEmotion = _settingsRepository.TtsVoiceEmotion;
            CardShowFrequency = _settingsRepository.CardShowFrequency.TotalMinutes;
            SyncBus = _localSettingsRepository.SyncBus;
            OpenSharedFolderCommand = new CorrelationCommand(() => _remembrancePathsProvider.OpenSharedFolder(_localSettingsRepository.SyncBus));
            OpenSettingsFolderCommand = new CorrelationCommand(_remembrancePathsProvider.OpenSettingsFolder);
            ViewLogsCommand = new CorrelationCommand(_remembrancePathsProvider.ViewLogs);
            SaveCommand = new CorrelationCommand(Save);
            ExportCommand = new AsyncCorrelationCommand(ExportAsync);
            ImportCommand = new AsyncCorrelationCommand(ImportAsync);
            WindowClosingCommand = new CorrelationCommand(WindowClosing);
            _cardsExchanger.Progress += CardsExchanger_Progress;
        }

        public IReadOnlyCollection<SyncBus> SyncBuses { get; }

        [NotNull]
        public IReadOnlyCollection<Language> AvailableTranslationLanguages { get; }

        [NotNull]
        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        [NotNull]
        public IReadOnlyCollection<Language> AvailableUiLanguages { get; } = new[]
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        [NotNull]
        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } =
            Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

        public double CardShowFrequency { get; set; }

        [NotNull]
        public ICommand ExportCommand { get; }

        [NotNull]
        public ICommand ImportCommand { get; }

        [NotNull]
        public ICommand OpenSettingsFolderCommand { get; }

        [NotNull]
        public ICommand OpenSharedFolderCommand { get; }

        [NotNull]
        public ProcessBlacklistViewModel ProcessBlacklistViewModel { get; }

        public int Progress { get; private set; }

        [CanBeNull]
        public string ProgressDescription { get; private set; }

        public TaskbarItemProgressState ProgressState { get; private set; }

        [NotNull]
        public ICommand SaveCommand { get; }

        public string SelectedPreferredLanguage { get; set; }

        public Speaker TtsSpeaker { get; set; }

        public SyncBus SyncBus { get; set; }

        public VoiceEmotion TtsVoiceEmotion { get; set; }

        public string UiLanguage
        {
            get => _uiLanguage;
            set
            {
                _uiLanguage = value;
                _messageHub.Publish(CultureInfo.GetCultureInfo(value));
            }
        }

        [NotNull]
        public ICommand ViewLogsCommand { get; }

        [NotNull]
        public ICommand WindowClosingCommand { get; }

        public void Dispose()
        {
            _cardsExchanger.Progress -= CardsExchanger_Progress;
        }

        public event EventHandler RequestClose;

        private void BeginProgress()
        {
            ProgressState = TaskbarItemProgressState.Normal;
            _pauseManager.Pause(PauseReason.OperationInProgress);
            ProgressDescription = "Caclulating...";
            Progress = 0;
        }

        private void CardsExchanger_Progress([NotNull] object sender, [NotNull] ProgressEventArgs e)
        {
            _synchronizationContext.Send(
                x =>
                {
                    Progress = e.Percentage;
                    ProgressDescription = $"{e.Current} of {e.Total} ({e.Percentage} %)";
                    if (e.Current == 0)
                    {
                        BeginProgress();
                    }
                    else if (e.Current == e.Total)
                    {
                        EndProgress();
                    }
                },
                null);
        }

        private void EndProgress()
        {
            ProgressState = TaskbarItemProgressState.None;
            _pauseManager.Resume(PauseReason.OperationInProgress);
        }

        [NotNull]
        private async Task ExportAsync()
        {
            await _cardsExchanger.ExportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        [NotNull]
        private async Task ImportAsync()
        {
            await _cardsExchanger.ImportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private void Save()
        {
            _logger.Trace("Saving settings...");
            if (_settingsRepository.PreferredLanguage != SelectedPreferredLanguage)
            {
                _settingsRepository.PreferredLanguage = SelectedPreferredLanguage;
            }

            if (_settingsRepository.TtsSpeaker != TtsSpeaker)
            {
                _settingsRepository.TtsSpeaker = TtsSpeaker;
            }

            if (_settingsRepository.TtsVoiceEmotion != TtsVoiceEmotion)
            {
                _settingsRepository.TtsVoiceEmotion = TtsVoiceEmotion;
            }

            if (_localSettingsRepository.UiLanguage != UiLanguage)
            {
                _localSettingsRepository.UiLanguage = UiLanguage;
            }

            var freq = TimeSpan.FromMinutes(CardShowFrequency);
            if (_settingsRepository.CardShowFrequency != freq)
            {
                _messageHub.Publish(freq);
                _settingsRepository.CardShowFrequency = freq;
            }

            if (_localSettingsRepository.SyncBus != SyncBus)
            {
                _messageHub.Publish(SyncBus);
                _localSettingsRepository.SyncBus = SyncBus;
            }

            _localSettingsRepository.BlacklistedProcesses = ProcessBlacklistViewModel.BlacklistedProcesses.Any() ? ProcessBlacklistViewModel.BlacklistedProcesses : null;

            _saved = true;

            RequestClose?.Invoke(null, null);
            _logger.Info("Settings has been saved");
        }

        private void WindowClosing()
        {
            if (!_saved)
            {
                _messageHub.Publish(CultureInfo.GetCultureInfo(_localSettingsRepository.UiLanguage));
            }

            _cancellationTokenSource.Cancel();
        }
    }
}