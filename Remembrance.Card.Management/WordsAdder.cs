using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Localization;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class WordsAdder : IWordsAdder
    {
        private static readonly string CurerntCultureLanguage = CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName;
        private static readonly string[] DefaultTargetLanguages = { Constants.EnLanguageTwoLetters, CurerntCultureLanguage == Constants.EnLanguageTwoLetters ? Constants.RuLanguageTwoLetters : CurerntCultureLanguage };

        [NotNull]
        private readonly ILanguageDetector languageDetector;

        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly ISettingsRepository settingsRepository;

        [NotNull]
        private readonly ITranslationDetailsRepository translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository translationEntryRepository;

        [NotNull]
        private readonly IWordsTranslator wordsTranslator;

        public WordsAdder(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsTranslator wordsTranslator,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ILog logger)
        {
            if (translationEntryRepository == null)
                throw new ArgumentNullException(nameof(translationEntryRepository));
            if (settingsRepository == null)
                throw new ArgumentNullException(nameof(settingsRepository));
            if (languageDetector == null)
                throw new ArgumentNullException(nameof(languageDetector));
            if (wordsTranslator == null)
                throw new ArgumentNullException(nameof(wordsTranslator));
            if (translationDetailsRepository == null)
                throw new ArgumentNullException(nameof(translationDetailsRepository));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.translationEntryRepository = translationEntryRepository;
            this.settingsRepository = settingsRepository;
            this.languageDetector = languageDetector;
            this.wordsTranslator = wordsTranslator;
            this.translationDetailsRepository = translationDetailsRepository;
            this.logger = logger;
        }

        public TranslationInfo AddWordWithChecks(string text, string sourceLanguage, string targetLanguage, bool allowExisting, int id)
        {
            logger.Debug($"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");
            if (string.IsNullOrWhiteSpace(text))
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);
            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
                //if not specified or autodetect - try to detect
                sourceLanguage = languageDetector.DetectLanguageAsync(text).Result.Language;
            if (sourceLanguage == null)
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);
            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)
                //if not specified - try to find best matching target language
                targetLanguage = GetDefaultTargetLanguage(sourceLanguage);

            //Used En as ui language to simplify conversion of common words to the enums
            var translationResult = wordsTranslator.GetTranslationAsync(sourceLanguage, targetLanguage, text, Constants.EnLanguage).Result;
            if (!translationResult.PartOfSpeechTranslations.Any())
                throw new LocalizableException($"No translations found for '{text}'", Errors.CannotTranslate);

            //replace the original text with the corrected one
            text = translationResult.PartOfSpeechTranslations.First().Text;

            var key = new TranslationEntryKey(text, sourceLanguage, targetLanguage);

            var translationEntry = translationEntryRepository.TryGetByKey(key);

            if (translationEntry != null)
                if (!allowExisting)
                    throw new LocalizableException($"An item with the same text ('{text}') and direction ({sourceLanguage}-{targetLanguage}) already exists", Errors.WordIsPresent);
                else
                    id = translationEntry.Id;

            translationEntry = new TranslationEntry(key, translationResult.GetDefaultWords()) { Id = id };

            logger.Debug($"Saving translation entry for {text} ({sourceLanguage} - {targetLanguage})...");
            id = translationEntryRepository.Save(translationEntry);
            var translationDetails = new TranslationDetails(translationResult) { Id = id };
            logger.Debug($"Saving translation details for {text} ({sourceLanguage} - {targetLanguage})...");
            translationDetailsRepository.Save(translationDetails);

            logger.Debug($"Translation for {text} ({sourceLanguage} - {targetLanguage}) has been successfully added");

            return new TranslationInfo(translationEntry, translationDetails);
        }

        public string GetDefaultTargetLanguage(string sourceLanguage)
        {
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));
            var settings = settingsRepository.Get();
            var lastUsedTargetLanguage = settings.LastUsedTargetLanguage;
            var possibleTargetLanguages = new Stack<string>(DefaultTargetLanguages);
            if (lastUsedTargetLanguage != null && lastUsedTargetLanguage != Constants.AutoDetectLanguage)
                possibleTargetLanguages.Push(lastUsedTargetLanguage); //top priority
            var targetLanguage = possibleTargetLanguages.Pop();
            while (targetLanguage == sourceLanguage)
                targetLanguage = possibleTargetLanguages.Pop();
            return targetLanguage;
        }
    }
}