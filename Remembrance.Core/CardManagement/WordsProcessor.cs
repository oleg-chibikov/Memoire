using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    // TODO: Think about class interface - it is not clear now
    [UsedImplicitly]
    internal sealed class WordsProcessor : IWordsProcessor
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
        private readonly ITranslationResultCardManager _cardManager;

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordsTranslator _wordsTranslator;

        public WordsProcessor(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ILog logger,
            [NotNull] ITranslationResultCardManager cardManager,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IWordsTranslator wordsTranslator,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector)
        {
            _wordsTranslator = wordsTranslator ?? throw new ArgumentNullException(nameof(wordsTranslator));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cardManager = cardManager ?? throw new ArgumentNullException(nameof(cardManager));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public TranslationDetails ReloadTranslationDetailsIfNeeded(object id, string text, string sourceLanguage, string targetLanguage)
        {
            var translationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(id);
            if (translationDetails != null)
                return translationDetails;

            var translationResult = Translate(text, sourceLanguage, targetLanguage);

            // There are no translation details for this word
            translationDetails = new TranslationDetails(translationResult, id);

            _logger.Trace($"Saving translation details for {id}...");
            _translationDetailsRepository.Save(translationDetails);

            return translationDetails;
        }

        public TranslationInfo AddOrChangeWord(string text, string sourceLanguage, string targetLanguage, IWindow ownerWindow, bool needPostProcess, object id)
        {
            _logger.Info($"Processing word {text}...");
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            // This method replaces translation with the actual one
            _logger.Trace($"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");

            var key = GetTranslationKey(text, sourceLanguage, targetLanguage);
            var translationResult = Translate(key.Text, key.SourceLanguage, key.TargetLanguage);

            // replace the original text with the corrected one
            key.Text = translationResult.PartOfSpeechTranslations.First().Text;
            var existingByKey = _translationEntryRepository.TryGetByKey(key);
            if (id != null)
            {
                if (!existingByKey?.Id.Equals(id) == true)
                    throw new LocalizableException($"An item with the same key {key} already exists", Errors.WordIsPresent);
            }
            else
            {
                if (existingByKey != null)
                    id = existingByKey.Id;
            }

            var translationEntry = new TranslationEntry(key)
            {
                Id = id
            };

            id = _translationEntryRepository.Save(translationEntry);

            var existingTranslationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(id);

            var translationDetails = new TranslationDetails(translationResult, id);
            if (existingTranslationDetails != null)
                translationDetails.Id = existingTranslationDetails.Id;

            _translationDetailsRepository.Save(translationDetails);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);

            _logger.Trace($"Translation for {key} has been successfully added");
            if (needPostProcess)
                PostProcessWord(ownerWindow, translationInfo);
            _logger.Trace($"Processing finished for word {text}");
            return translationInfo;
        }

        public string GetDefaultTargetLanguage(string sourceLanguage)
        {
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));

            var settings = _settingsRepository.Get();
            var lastUsedTargetLanguage = settings.LastUsedTargetLanguage;
            var possibleTargetLanguages = new Stack<string>(DefaultTargetLanguages);
            if (lastUsedTargetLanguage != null && lastUsedTargetLanguage != Constants.AutoDetectLanguage)
                possibleTargetLanguages.Push(lastUsedTargetLanguage); // top priority
            var targetLanguage = possibleTargetLanguages.Pop();
            while (targetLanguage == sourceLanguage)
                targetLanguage = possibleTargetLanguages.Pop();

            return targetLanguage;
        }

        [NotNull]
        private TranslationEntryKey GetTranslationKey([NotNull] string text, [CanBeNull] string sourceLanguage, [CanBeNull] string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)

                // if not specified or autodetect - try to detect
                sourceLanguage = _languageDetector.DetectLanguageAsync(text, CancellationToken.None).Result.Language;
            if (sourceLanguage == null)
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);

            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)

                // if not specified - try to find best matching target language
                targetLanguage = GetDefaultTargetLanguage(sourceLanguage);

            return new TranslationEntryKey(text, sourceLanguage, targetLanguage);
        }

        private void PostProcessWord([CanBeNull] IWindow ownerWindow, [NotNull] TranslationInfo translationInfo)
        {
            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);
            _messenger.Publish(translationInfo);
            _cardManager.ShowCard(translationInfo, ownerWindow);
        }

        [NotNull]
        private TranslationResult Translate([NotNull] string text, [NotNull] string sourceLanguage, [NotNull] string targetLanguage)
        {
            // Used En as ui language to simplify conversion of common words to the enums
            var translationResult = _wordsTranslator.GetTranslationAsync(sourceLanguage, targetLanguage, text, Constants.EnLanguage).Result;
            if (!translationResult.PartOfSpeechTranslations.Any())
                throw new LocalizableException($"No translations found for {text}", string.Format(Errors.CannotTranslate, text, sourceLanguage, targetLanguage));
            _logger.Trace("Received translation");

            return translationResult;
        }
    }
}