using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
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
using Remembrance.Contracts.View;
using Scar.Common.Events;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class SettingsViewModel : BaseViewModel
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ICardsExchanger _cardsExchanger;

        private readonly ILocalSettingsRepository _localSettingsRepository;

        private readonly ILog _logger;

        private readonly IMessageHub _messageHub;

        private readonly IPauseManager _pauseManager;

        private readonly ISettingsRepository _settingsRepository;

        private readonly SynchronizationContext _synchronizationContext;

        private bool _saved;

        private string _uiLanguage;

        public SettingsViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ISettingsRepository settingsRepository,
            ILog logger,
            IMessageHub messageHub,
            ICardsExchanger cardsExchanger,
            SynchronizationContext synchronizationContext,
            IPauseManager pauseManager,
            ProcessBlacklistViewModel processBlacklistViewModel,
            ILanguageManager languageManager,
            IRemembrancePathsProvider remembrancePathsProvider,
            ICommandManager commandManager)
            : base(commandManager)
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
            _ = remembrancePathsProvider ?? throw new ArgumentNullException(nameof(remembrancePathsProvider));

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
            if (remembrancePathsProvider.DropBoxPath != null)
            {
                syncBuses.Add(SyncBus.Dropbox);
            }

            if (remembrancePathsProvider.OneDrivePath != null)
            {
                syncBuses.Add(SyncBus.OneDrive);
            }

            SyncBuses = syncBuses.ToArray();
            AvailableTranslationLanguages = languageManager.GetAvailableSourceLanguages(false);
            SelectedPreferredLanguage = _settingsRepository.PreferredLanguage;
            TtsSpeaker = _settingsRepository.TtsSpeaker;
            _uiLanguage = _localSettingsRepository.UiLanguage;
            UiLanguage = _uiLanguage;
            TtsVoiceEmotion = _settingsRepository.TtsVoiceEmotion;
            CardShowFrequency = _settingsRepository.CardShowFrequency.TotalMinutes;
            SolveQwantCaptcha = _settingsRepository.SolveQwantCaptcha;
            SyncBus = _localSettingsRepository.SyncBus;
            OpenSharedFolderCommand = AddCommand(() => remembrancePathsProvider.OpenSharedFolder(_localSettingsRepository.SyncBus));
            OpenSettingsFolderCommand = AddCommand(remembrancePathsProvider.OpenSettingsFolder);
            ViewLogsCommand = AddCommand(remembrancePathsProvider.ViewLogs);
            SaveCommand = AddCommand(Save);
            ExportCommand = AddCommand(ExportAsync);
            ImportCommand = AddCommand(ImportAsync);
            WindowClosingCommand = AddCommand(WindowClosing);
            _cardsExchanger.Progress += CardsExchanger_Progress;
        }

        public IReadOnlyCollection<SyncBus> SyncBuses { get; }

        public IReadOnlyCollection<Language> AvailableTranslationLanguages { get; }

        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        public IEnumerable<Language> AvailableUiLanguages { get; } = new[]
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } =
            Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

        public double CardShowFrequency { get; set; }

        public bool SolveQwantCaptcha { get; set; }

        public ICommand ExportCommand { get; }

        public ICommand ImportCommand { get; }

        public ICommand OpenSettingsFolderCommand { get; }

        public ICommand OpenSharedFolderCommand { get; }

        public ProcessBlacklistViewModel ProcessBlacklistViewModel { get; }

        public int Progress { get; private set; }

        public string? ProgressDescription { get; private set; }

        public ProgressState ProgressState { get; private set; }

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

        public ICommand ViewLogsCommand { get; }

        public ICommand WindowClosingCommand { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _cardsExchanger.Progress -= CardsExchanger_Progress;
            }
        }

        private void BeginProgress()
        {
            ProgressState = ProgressState.Normal;
            _pauseManager.Pause(PauseReason.OperationInProgress);
            ProgressDescription = "Caclulating...";
            Progress = 0;
        }

        private void CardsExchanger_Progress(object sender, ProgressEventArgs e)
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
            ProgressState = ProgressState.None;
            _pauseManager.Resume(PauseReason.OperationInProgress);
        }

        private async Task ExportAsync()
        {
            await _cardsExchanger.ExportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

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

            if (_settingsRepository.SolveQwantCaptcha != SolveQwantCaptcha)
            {
                _settingsRepository.SolveQwantCaptcha = SolveQwantCaptcha;
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

            CloseWindow();
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