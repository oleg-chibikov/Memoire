using System;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;
using Remembrance.Resources;
using Remembrance.ViewModel.Settings.Data;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

//TODO: Feature: Custom translation
namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseViewModelWithAddTranslationControl
    {
        protected BaseViewModelWithAddTranslationControl([NotNull] ISettingsRepository settingsRepository, [NotNull] ILanguageDetector languageDetector, [NotNull] IWordsProcessor wordsProcessor, [NotNull] ILog logger)
        {
            if (languageDetector == null)
                throw new ArgumentNullException(nameof(languageDetector));

            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            WordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            SaveCommand = new CorrelationCommand<string>(Save);

            logger.Trace("Loading settings...");

            var settings = settingsRepository.Get();

            logger.Trace("Loading languages...");
            var languages = languageDetector.ListLanguagesAsync(CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName).Result;

            //var acceptableLanguages = languages.Directions.Where(x => x.StartsWith(UiLanguage)).Select(x => x.Split('-')[1]).Concat(new []{UiLanguage}).ToArray();
            //AvailableTargetLanguages = languages.Languages.Where(x => acceptableLanguages.Contains(x.Key)).Select(x => new Language(x.Key, x.Value)).ToArray();

            var availableLanguages = languages.Languages.Select(x => new Language(x.Key, x.Value)).OrderBy(x => x.DisplayName);

            var autoSourceLanguage = new Language(Constants.AutoDetectLanguage, "--AutoDetect--");
            var autoTargetLanguage = new Language(Constants.AutoDetectLanguage, "--Reverse--");

            AvailableTargetLanguages = new[]
                {
                    autoTargetLanguage
                }.Concat(availableLanguages)
                .ToArray();
            AvailableSourceLanguages = new[]
                {
                    autoSourceLanguage
                }.Concat(availableLanguages)
                .ToArray();

            Language targetLanguage = null;
            if (settings.LastUsedTargetLanguage != null)
                targetLanguage = AvailableTargetLanguages.SingleOrDefault(x => x.Code == settings.LastUsedTargetLanguage);
            if (targetLanguage == null)
                targetLanguage = autoTargetLanguage;
            _selectedTargetLanguage = targetLanguage;

            Language sourceLanguage = null;
            if (settings.LastUsedSourceLanguage != null)
                sourceLanguage = AvailableSourceLanguages.SingleOrDefault(x => x.Code == settings.LastUsedSourceLanguage);
            if (sourceLanguage == null)
                sourceLanguage = autoSourceLanguage;
            _selectedSourceLanguage = sourceLanguage;
            logger.Trace("Languages have been loaded");
        }

        protected abstract IWindow Window { get; }

        public Language[] AvailableTargetLanguages { get; }

        public Language[] AvailableSourceLanguages { get; }

        #region Commands

        public ICommand SaveCommand { get; }

        #endregion

        #region Command handlers

        private void Save([NotNull] string text)
        {
            Logger.Info($"Adding translation for {text}...");
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            NewItemSource = null;

            var sourceLanguage = SelectedSourceLanguage.Code;
            var targetLanguage = SelectedTargetLanguage.Code;

            WordsProcessor.ProcessNewWord(text, sourceLanguage, targetLanguage, Window);
        }

        #endregion

        #region Dependencies

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly IWordsProcessor WordsProcessor;

        #endregion

        #region Dependency properties

        private Language _selectedTargetLanguage;

        public Language SelectedTargetLanguage
        {
            get { return _selectedTargetLanguage; }
            [UsedImplicitly]
            set
            {
                //TODO: Transactions?
                _selectedTargetLanguage = value;
                var settings = _settingsRepository.Get();
                settings.LastUsedTargetLanguage = value.Code;
                _settingsRepository.Save(settings);
            }
        }

        private Language _selectedSourceLanguage;

        public Language SelectedSourceLanguage
        {
            get { return _selectedSourceLanguage; }
            [UsedImplicitly]
            set
            {
                //TODO: Transactions - https://github.com/mbdavid/LiteDB/wiki/Transactions-and-Concurrency? across all solution
                _selectedSourceLanguage = value;
                var settings = _settingsRepository.Get();
                settings.LastUsedSourceLanguage = value.Code;
                _settingsRepository.Save(settings);
            }
        }

        public string NewItemSource
        {
            get;
            [UsedImplicitly]
            set;
        }

        #endregion
    }
}