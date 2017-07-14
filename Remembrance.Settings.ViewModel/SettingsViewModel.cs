using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Resources;
using Remembrance.Settings.ViewModel.Contracts;
using Remembrance.Settings.ViewModel.Contracts.Data;
using Remembrance.Translate.Contracts.Data.TextToSpeechPlayer;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    public sealed class SettingsViewModel : ViewModelBase, ISettingsViewModel, IRequestCloseViewModel
    {
        [NotNull]
        private readonly ICardsExchanger _cardsExchanger;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

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
        }

        public event EventHandler RequestClose;

        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } = Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

        public Language[] AvailableUiLanguages { get; } =
        {
            new Language(Constants.EnLanguage, "English"),
            new Language(Constants.RuLanguage, "Русский")
        };

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand ViewLogsCommand { get; }
        public ICommand OpenSharedFolderCommand { get; }
        public ICommand OpenSettingsFolderCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }

        #endregion

        #region Command Handlers

        private void Save()
        {
            _logger.Debug("Saving settings...");
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
            _logger.Debug("Settings has been saved");
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
            _cardsExchanger.Export();
        }

        private void Import()
        {
            _cardsExchanger.Import();
        }

        #endregion

        #region DependencyProperties

        private double _cardShowFrequency;

        public double CardShowFrequency
        {
            get { return _cardShowFrequency; }
            [UsedImplicitly]
            set { Set(() => CardShowFrequency, ref _cardShowFrequency, value); }
        }

        private Language _uiLanguage;

        public Language UiLanguage
        {
            get { return _uiLanguage; }
            [UsedImplicitly]
            set
            {
                Set(() => UiLanguage, ref _uiLanguage, value);
                _messenger.Send(_uiLanguage.Code, MessengerTokens.UiLanguageToken);
            }
        }

        private Speaker _ttsSpeaker;

        public Speaker TtsSpeaker
        {
            get { return _ttsSpeaker; }
            [UsedImplicitly]
            set { Set(() => TtsSpeaker, ref _ttsSpeaker, value); }
        }

        private VoiceEmotion _ttsVoiceEmotion;

        public VoiceEmotion TtsVoiceEmotion
        {
            get { return _ttsVoiceEmotion; }
            [UsedImplicitly]
            set { Set(() => TtsVoiceEmotion, ref _ttsVoiceEmotion, value); }
        }

        private bool _reverseTranslation;

        public bool ReverseTranslation
        {
            get { return _reverseTranslation; }
            [UsedImplicitly]
            set { Set(() => ReverseTranslation, ref _reverseTranslation, value); }
        }

        private bool _randomTranslation;

        public bool RandomTranslation
        {
            get { return _randomTranslation; }
            [UsedImplicitly]
            set { Set(() => RandomTranslation, ref _randomTranslation, value); }
        }

        #endregion
    }
}