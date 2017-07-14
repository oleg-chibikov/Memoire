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

        private static readonly string[] DefaultTargetLanguages =
        {
            Constants.EnLanguageTwoLetters,
            CurerntCultureLanguage == Constants.EnLanguageTwoLetters
                ? Constants.RuLanguageTwoLetters
                : CurerntCultureLanguage
        };

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordsTranslator _wordsTranslator;

        public WordsAdder(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsTranslator wordsTranslator,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ILog logger)
        {
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _wordsTranslator = wordsTranslator ?? throw new ArgumentNullException(nameof(wordsTranslator));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public TranslationInfo AddWordWithChecks(string text, string sourceLanguage, string targetLanguage, bool allowExisting, int id)
        {
            _logger.Debug($"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");
            if (string.IsNullOrWhiteSpace(text))
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
                //if not specified or autodetect - try to detect
                sourceLanguage = _languageDetector.DetectLanguageAsync(text).Result.Language;
            if (sourceLanguage == null)
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);

            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)
                //if not specified - try to find best matching target language
                targetLanguage = GetDefaultTargetLanguage(sourceLanguage);

            //Used En as ui language to simplify conversion of common words to the enums
            var translationResult = _wordsTranslator.GetTranslationAsync(sourceLanguage, targetLanguage, text, Constants.EnLanguage).Result;
            if (!translationResult.PartOfSpeechTranslations.Any())
                throw new LocalizableException($"No translations found for '{text}'", Errors.CannotTranslate);

            //replace the original text with the corrected one
            text = translationResult.PartOfSpeechTranslations.First().Text;

            var key = new TranslationEntryKey(text, sourceLanguage, targetLanguage);

            var translationEntry = _translationEntryRepository.TryGetByKey(key);

            if (translationEntry != null)
                if (!allowExisting)
                    throw new LocalizableException($"An item with the same text ('{text}') and direction ({sourceLanguage}-{targetLanguage}) already exists", Errors.WordIsPresent);
                else
                    id = translationEntry.Id;

            translationEntry = new TranslationEntry(key, translationResult.GetDefaultWords())
            {
                Id = id
            };

            _logger.Debug($"Saving translation entry for {text} ({sourceLanguage} - {targetLanguage})...");
            id = _translationEntryRepository.Save(translationEntry);
            var translationDetails = new TranslationDetails(translationResult)
            {
                Id = id
            };
            _logger.Debug($"Saving translation details for {text} ({sourceLanguage} - {targetLanguage})...");
            _translationDetailsRepository.Save(translationDetails);

            _logger.Debug($"Translation for {text} ({sourceLanguage} - {targetLanguage}) has been successfully added");

            return new TranslationInfo(translationEntry, translationDetails);
        }

        public string GetDefaultTargetLanguage(string sourceLanguage)
        {
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));

            var settings = _settingsRepository.Get();
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