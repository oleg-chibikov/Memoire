using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
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
        readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        readonly ICardsExchanger _cardsExchanger;

        readonly ILocalSettingsRepository _localSettingsRepository;

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        readonly IPauseManager _pauseManager;

        readonly ISharedSettingsRepository _sharedSettingsRepository;

        readonly SynchronizationContext _synchronizationContext;

        bool _saved;

        string _uiLanguage;

        public SettingsViewModel(
            ILocalSettingsRepository localSettingsRepository,
            ISharedSettingsRepository sharedSettingsRepository,
            ILogger<SettingsViewModel> logger,
            IMessageHub messageHub,
            ICardsExchanger cardsExchanger,
            SynchronizationContext synchronizationContext,
            IPauseManager pauseManager,
            ProcessBlacklistViewModel processBlacklistViewModel,
            ILanguageManager languageManager,
            IRemembrancePathsProvider remembrancePathsProvider,
            ICommandManager commandManager) : base(commandManager)
        {
            _ = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
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

            IList<SyncEngine> syncBuses = new List<SyncEngine> { SyncEngine.NoSync };
            if (remembrancePathsProvider.DropBoxPath != null)
            {
                syncBuses.Add(SyncEngine.DropBox);
            }

            if (remembrancePathsProvider.OneDrivePath != null)
            {
                syncBuses.Add(SyncEngine.OneDrive);
            }

            SyncBuses = syncBuses.ToArray();
            AvailableTranslationLanguages = languageManager.GetAvailableSourceLanguages(false);
            SelectedPreferredLanguage = _sharedSettingsRepository.PreferredLanguage;
            TtsSpeaker = _sharedSettingsRepository.TtsSpeaker;
            TtsVoiceEmotion = _sharedSettingsRepository.TtsVoiceEmotion;
            CardShowFrequency = _sharedSettingsRepository.CardShowFrequency.TotalMinutes;
            SolveQwantCaptcha = _sharedSettingsRepository.SolveQwantCaptcha;
            CardsToShowAtOnce = _sharedSettingsRepository.CardsToShowAtOnce;
            UiLanguage = _uiLanguage = _localSettingsRepository.UiLanguage;
            SyncEngine = _localSettingsRepository.SyncEngine;
            OpenSharedFolderCommand = AddCommand(() => remembrancePathsProvider.OpenSharedFolder(_localSettingsRepository.SyncEngine));
            OpenSettingsFolderCommand = AddCommand(remembrancePathsProvider.OpenSettingsFolder);
            ViewLogsCommand = AddCommand(remembrancePathsProvider.ViewLogs);
            SaveCommand = AddCommand(Save);
            ExportCommand = AddCommand(ExportAsync);
            ImportCommand = AddCommand(ImportAsync);
            WindowClosingCommand = AddCommand(WindowClosing);
            _cardsExchanger.Progress += CardsExchanger_Progress;
        }

        public IReadOnlyCollection<SyncEngine> SyncBuses { get; }

        public IReadOnlyCollection<Language> AvailableTranslationLanguages { get; }

        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        public IEnumerable<Language> AvailableUiLanguages { get; } = new[]
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } = Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

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

        public int CardsToShowAtOnce { get; set; }

        public SyncEngine SyncEngine { get; set; }

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
                _cancellationTokenSource.Dispose();
            }
        }

        void BeginProgress()
        {
            ProgressState = ProgressState.Normal;
            _pauseManager.PauseActivity(PauseReasons.OperationInProgress);
            ProgressDescription = "Calculating...";
            Progress = 0;
        }

        void CardsExchanger_Progress(object sender, ProgressEventArgs e)
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

        void EndProgress()
        {
            ProgressState = ProgressState.None;
            _pauseManager.ResumeActivity(PauseReasons.OperationInProgress);
        }

        async Task ExportAsync()
        {
            await _cardsExchanger.ExportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        async Task ImportAsync()
        {
            await _cardsExchanger.ImportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        void Save()
        {
            _logger.LogTrace("Saving settings...");
            if (_sharedSettingsRepository.PreferredLanguage != SelectedPreferredLanguage)
            {
                _sharedSettingsRepository.PreferredLanguage = SelectedPreferredLanguage;
            }

            if (_sharedSettingsRepository.TtsSpeaker != TtsSpeaker)
            {
                _sharedSettingsRepository.TtsSpeaker = TtsSpeaker;
            }

            if (_sharedSettingsRepository.TtsVoiceEmotion != TtsVoiceEmotion)
            {
                _sharedSettingsRepository.TtsVoiceEmotion = TtsVoiceEmotion;
            }

            if (_sharedSettingsRepository.SolveQwantCaptcha != SolveQwantCaptcha)
            {
                _sharedSettingsRepository.SolveQwantCaptcha = SolveQwantCaptcha;
            }

            if (_sharedSettingsRepository.CardsToShowAtOnce != CardsToShowAtOnce)
            {
                _sharedSettingsRepository.CardsToShowAtOnce = CardsToShowAtOnce;
            }

            if (_localSettingsRepository.UiLanguage != UiLanguage)
            {
                _localSettingsRepository.UiLanguage = UiLanguage;
            }

            var freq = TimeSpan.FromMinutes(CardShowFrequency);
            if (_sharedSettingsRepository.CardShowFrequency != freq)
            {
                _messageHub.Publish(freq);
                _sharedSettingsRepository.CardShowFrequency = freq;
            }

            if (_localSettingsRepository.SyncEngine != SyncEngine)
            {
                _messageHub.Publish(SyncEngine);
                _localSettingsRepository.SyncEngine = SyncEngine;
            }

            _localSettingsRepository.BlacklistedProcesses = (ProcessBlacklistViewModel.BlacklistedProcesses.Count > 0) ? ProcessBlacklistViewModel.BlacklistedProcesses : null;

            _saved = true;

            CloseWindow();
            _logger.LogInformation("Settings has been saved");
        }

        void WindowClosing()
        {
            if (!_saved)
            {
                _messageHub.Publish(CultureInfo.GetCultureInfo(_localSettingsRepository.UiLanguage));
            }

            _cancellationTokenSource.Cancel();
        }
    }
}
