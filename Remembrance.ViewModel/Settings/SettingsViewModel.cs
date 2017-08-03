using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows.Shell;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
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
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        public SettingsViewModel([NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger, [NotNull] IMessenger messenger, [NotNull] ICardsExchanger cardsExchanger)
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

        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } = Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

        public Language[] AvailableUiLanguages { get; } =
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        public void Dispose()
        {
            _cardsExchanger.Progress -= CardsExchanger_Progress;
        }

        public event EventHandler RequestClose;

        #region Commands

        public ICommand SaveCommand { get; }

        public ICommand ViewLogsCommand { get; }

        public ICommand OpenSharedFolderCommand { get; }

        public ICommand OpenSettingsFolderCommand { get; }

        public ICommand ExportCommand { get; }

        public ICommand ImportCommand { get; }

        public ICommand WindowClosingCommand { get; }

        #endregion

        #region Command Handlers

        private void Save()
        {
            _logger.Trace("Saving settings...");
            var freq = TimeSpan.FromMinutes(CardShowFrequency);
            var settings = _settingsRepository.Get();
            var prevFreq = settings.CardShowFrequency;
            settings.CardShowFrequency = freq;
            settings.TtsSpeaker = TtsSpeaker;
            settings.TtsVoiceEmotion = TtsVoiceEmotion;
            settings.ReverseTranslation = ReverseTranslation;
            settings.RandomTranslation = RandomTranslation;
            settings.UiLanguage = UiLanguage.Code;
            _settingsRepository.Save(settings);
            if (prevFreq != freq)
                _messenger.Send(settings.CardShowFrequency, MessengerTokens.CardShowFrequencyToken);
            RequestClose?.Invoke(null, null);
            _logger.Trace("Settings has been saved");
        }

        private void BeginProgress()
        {
            ProgressState = TaskbarItemProgressState.Normal;
            ProgressDescription = "Caclulating...";
            Progress = 0;
        }

        private void EndProgress()
        {
            ProgressState = TaskbarItemProgressState.None;
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

        private static void OpenSharedFolder()
        {
            Process.Start($@"{Paths.SharedDataPath}");
        }

        private static void OpenSettingsFolder()
        {
            Process.Start($@"{Paths.SettingsPath}");
        }

        private static void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }

        private void Export()
        {
            _cardsExchanger.ExportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private void Import()
        {
            _cardsExchanger.ImportAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private void WindowClosing()
        {
            _cancellationTokenSource.Cancel();
        }

        #endregion

        #region DependencyProperties

        public int Progress { get; private set; }

        public string ProgressDescription { get; private set; }

        public TaskbarItemProgressState ProgressState { get; private set; }

        private Language _uiLanguage;

        public Language UiLanguage
        {
            get { return _uiLanguage; }
            [UsedImplicitly]
            set
            {
                _uiLanguage = value;
                _messenger.Send(_uiLanguage.Code, MessengerTokens.UiLanguageToken);
            }
        }

        public double CardShowFrequency
        {
            get;
            [UsedImplicitly]
            set;
        }

        public Speaker TtsSpeaker
        {
            get;
            [UsedImplicitly]
            set;
        }

        public VoiceEmotion TtsVoiceEmotion
        {
            get;
            [UsedImplicitly]
            set;
        }

        public bool ReverseTranslation
        {
            get;
            [UsedImplicitly]
            set;
        }

        public bool RandomTranslation
        {
            get;
            [UsedImplicitly]
            set;
        }

        #endregion
    }
}