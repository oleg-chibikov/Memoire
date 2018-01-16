using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Shell;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;
using Remembrance.ViewModel.Settings.Data;
using Scar.Common.Events;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.ViewModel.Settings
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
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        private Language _uiLanguage;

        public SettingsViewModel([NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger, [NotNull] IMessageHub messenger, [NotNull] ICardsExchanger cardsExchanger)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _cardsExchanger = cardsExchanger ?? throw new ArgumentNullException(nameof(cardsExchanger));

            var settings = settingsRepository.Get();
            TtsSpeaker = settings.TtsSpeaker;
            UiLanguage = AvailableUiLanguages.Single(x => x.Code == settings.UiLanguage);
            TtsVoiceEmotion = settings.TtsVoiceEmotion;
            ReverseTranslation = settings.ReverseTranslation;
            RandomTranslation = settings.RandomTranslation;
            CardShowFrequency = settings.CardShowFrequency.TotalMinutes;
            TranslationCloseTimeout = settings.TranslationCloseTimeout.TotalSeconds;
            AssessmentSuccessCloseTimeout = settings.AssessmentSuccessCloseTimeout.TotalSeconds;
            AssessmentFailureCloseTimeout = settings.AssessmentFailureCloseTimeout.TotalSeconds;
            OpenSharedFolderCommand = new CorrelationCommand(OpenSharedFolder);
            OpenSettingsFolderCommand = new CorrelationCommand(OpenSettingsFolder);
            SaveCommand = new CorrelationCommand(Save);
            SaveCommand = new CorrelationCommand(Save);
            ViewLogsCommand = new CorrelationCommand(ViewLogs);
            ExportCommand = new CorrelationCommand(Export);
            ImportCommand = new CorrelationCommand(Import);
            WindowClosingCommand = new CorrelationCommand(WindowClosing);
            _cardsExchanger.Progress += CardsExchanger_Progress;
        }

        [NotNull]
        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker))
            .Cast<Speaker>()
            .ToDictionary(x => x, x => x.ToString());

        [NotNull]
        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } = Enum.GetValues(typeof(VoiceEmotion))
            .Cast<VoiceEmotion>()
            .ToDictionary(x => x, x => x.ToString());

        [NotNull]
        public Language[] AvailableUiLanguages { get; } =
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        [NotNull]
        public ICommand SaveCommand { get; }

        [NotNull]
        public ICommand ViewLogsCommand { get; }

        [NotNull]
        public ICommand OpenSharedFolderCommand { get; }

        [NotNull]
        public ICommand OpenSettingsFolderCommand { get; }

        [NotNull]
        public ICommand ExportCommand { get; }

        [NotNull]
        public ICommand ImportCommand { get; }

        [NotNull]
        public ICommand WindowClosingCommand { get; }

        public int Progress { get; private set; }

        [CanBeNull]
        public string ProgressDescription { get; private set; }

        public TaskbarItemProgressState ProgressState { get; private set; }

        public Language UiLanguage
        {
            get => _uiLanguage;
            set
            {
                _uiLanguage = value;
                _messenger.Publish(CultureInfo.GetCultureInfo(value.Code));
            }
        }

        public double CardShowFrequency { get; set; }

        public double TranslationCloseTimeout { get; set; }

        public double AssessmentSuccessCloseTimeout { get; set; }

        public double AssessmentFailureCloseTimeout { get; set; }

        public Speaker TtsSpeaker { get; set; }

        public VoiceEmotion TtsVoiceEmotion { get; set; }

        public bool ReverseTranslation { get; set; }

        public bool RandomTranslation { get; set; }

        public void Dispose()
        {
            _cardsExchanger.Progress -= CardsExchanger_Progress;
        }

        public event EventHandler RequestClose;

        private void BeginProgress()
        {
            ProgressState = TaskbarItemProgressState.Normal;
            _logger.Trace("Pausing showing cards...");
            _messenger.Publish(IntervalModificator.Pause);
            ProgressDescription = "Caclulating...";
            Progress = 0;
        }

        private void CardsExchanger_Progress([NotNull] object sender, [NotNull] ProgressEventArgs e)
        {
            _syncContext.Send(
                x =>
                {
                    Progress = e.Percentage;
                    ProgressDescription = $"{e.Current} of {e.Total} ({e.Percentage} %)";
                    if (e.Current == 0)
                        BeginProgress();
                    else if (e.Current == e.Total)
                        EndProgress();
                },
                null);
        }

        private void EndProgress()
        {
            ProgressState = TaskbarItemProgressState.None;
            _logger.Trace("Resuming showing cards...");
            _messenger.Publish(IntervalModificator.Resume);
        }

        private void Export()
        {
            _cardsExchanger.ExportAsync(_cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private void Import()
        {
            _cardsExchanger.ImportAsync(_cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private static void OpenSettingsFolder()
        {
            Process.Start($@"{Paths.SettingsPath}");
        }

        private static void OpenSharedFolder()
        {
            Process.Start($@"{Paths.SharedDataPath}");
        }

        private void Save()
        {
            _logger.Trace("Saving settings...");
            var freq = TimeSpan.FromMinutes(CardShowFrequency);
            var settings = _settingsRepository.Get();
            var prevFreq = settings.CardShowFrequency;
            settings.CardShowFrequency = freq;
            settings.AssessmentSuccessCloseTimeout = TimeSpan.FromSeconds(AssessmentSuccessCloseTimeout);
            settings.AssessmentFailureCloseTimeout = TimeSpan.FromSeconds(AssessmentFailureCloseTimeout);
            settings.TranslationCloseTimeout = TimeSpan.FromSeconds(TranslationCloseTimeout);
            settings.TtsSpeaker = TtsSpeaker;
            settings.TtsVoiceEmotion = TtsVoiceEmotion;
            settings.ReverseTranslation = ReverseTranslation;
            settings.RandomTranslation = RandomTranslation;
            settings.UiLanguage = UiLanguage.Code;
            _settingsRepository.Save(settings);
            if (prevFreq != freq)
                _messenger.Publish(settings.CardShowFrequency);
            RequestClose?.Invoke(null, null);
            _logger.Trace("Settings has been saved");
        }

        private static void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }

        private void WindowClosing()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}