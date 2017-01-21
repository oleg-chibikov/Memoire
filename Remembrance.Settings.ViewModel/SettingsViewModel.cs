﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Resources;
using Remembrance.Settings.ViewModel.Contracts;
using Remembrance.Settings.ViewModel.Contracts.Data;
using Remembrance.Translate.Contracts.Data.TextToSpeechPlayer;
using Scar.Common.WPF;

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    public sealed class SettingsViewModel : ViewModelBase, ISettingsViewModel, IRequestCloseViewModel
    {
        [NotNull]
        private readonly ICardsExchanger cardsExchanger;

        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly IMessenger messenger;

        [NotNull]
        private readonly ISettingsRepository settingsRepository;

        public SettingsViewModel(
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] IMessenger messenger,
            [NotNull] ICardsExchanger cardsExchanger)
        {
            if (settingsRepository == null)
                throw new ArgumentNullException(nameof(settingsRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            if (cardsExchanger == null)
                throw new ArgumentNullException(nameof(cardsExchanger));

            this.settingsRepository = settingsRepository;
            this.logger = logger;
            this.messenger = messenger;
            this.cardsExchanger = cardsExchanger;

            var settings = settingsRepository.Get();
            TtsSpeaker = settings.TtsSpeaker;
            UiLanguage = AvailableUiLanguages.Single(x => x.Code == settings.UiLanguage);
            TtsVoiceEmotion = settings.TtsVoiceEmotion;
            ReverseTranslation = settings.ReverseTranslation;
            RandomTranslation = settings.RandomTranslation;
            CardShowFrequency = settings.CardShowFrequency.TotalMinutes;
            OpenSharedFolderCommand = new RelayCommand(OpenSharedFolder);
            OpenSettingsFolderCommand = new RelayCommand(OpenSettingsFolder);
            SaveCommand = new RelayCommand(Save);
            SaveCommand = new RelayCommand(Save);
            ViewLogsCommand = new RelayCommand(ViewLogs);
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
        }

        public event EventHandler RequestClose;

        public IDictionary<Speaker, string> AvailableTtsSpeakers { get; } = Enum.GetValues(typeof(Speaker)).Cast<Speaker>().ToDictionary(x => x, x => x.ToString());

        public IDictionary<VoiceEmotion, string> AvailableVoiceEmotions { get; } = Enum.GetValues(typeof(VoiceEmotion)).Cast<VoiceEmotion>().ToDictionary(x => x, x => x.ToString());

        public Language[] AvailableUiLanguages { get; } = { new Language(Constants.EnLanguage, "English"), new Language(Constants.RuLanguage, "Русский") };

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
            logger.Debug("Saving settings...");
            var freq = TimeSpan.FromMinutes(CardShowFrequency);
            var settings = settingsRepository.Get();
            var prevFreq = settings.CardShowFrequency;
            settings.CardShowFrequency = freq;
            settings.TtsSpeaker = TtsSpeaker;
            settings.TtsVoiceEmotion = TtsVoiceEmotion;
            settings.ReverseTranslation = ReverseTranslation;
            settings.RandomTranslation = RandomTranslation;
            settings.UiLanguage = UiLanguage.Code;
            settingsRepository.Save(settings);
            if (prevFreq != freq)
                messenger.Send(settings.CardShowFrequency, MessengerTokens.CardShowFrequencyToken);
            RequestClose?.Invoke(null, null);
            logger.Debug("Settings has been saved");
        }

        private static void OpenSharedFolder()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            Process.Start($@"{Paths.SharedDataPath}");
        }

        private static void OpenSettingsFolder()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            Process.Start($@"{Paths.SettingsPath}");
        }

        private static void ViewLogs()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }

        private void Export()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            cardsExchanger.Export();
        }

        private void Import()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            cardsExchanger.Import();
        }

        #endregion

        #region DependencyProperties

        private double cardShowFrequency;

        public double CardShowFrequency
        {
            get { return cardShowFrequency; }
            [UsedImplicitly]
            set { Set(() => CardShowFrequency, ref cardShowFrequency, value); }
        }

        private Language uiLanguage;

        public Language UiLanguage
        {
            get { return uiLanguage; }
            [UsedImplicitly]
            set
            {
                Set(() => UiLanguage, ref uiLanguage, value);
                messenger.Send(uiLanguage.Code, MessengerTokens.UiLanguageToken);
            }
        }

        private Speaker ttsSpeaker;

        public Speaker TtsSpeaker
        {
            get { return ttsSpeaker; }
            [UsedImplicitly]
            set { Set(() => TtsSpeaker, ref ttsSpeaker, value); }
        }

        private VoiceEmotion ttsVoiceEmotion;

        public VoiceEmotion TtsVoiceEmotion
        {
            get { return ttsVoiceEmotion; }
            [UsedImplicitly]
            set { Set(() => TtsVoiceEmotion, ref ttsVoiceEmotion, value); }
        }


        private bool reverseTranslation;

        public bool ReverseTranslation
        {
            get { return reverseTranslation; }
            [UsedImplicitly]
            set { Set(() => ReverseTranslation, ref reverseTranslation, value); }
        }

        private bool randomTranslation;

        public bool RandomTranslation
        {
            get { return randomTranslation; }
            [UsedImplicitly]
            set { Set(() => RandomTranslation, ref randomTranslation, value); }
        }

        #endregion
    }
}