using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Localization;

namespace Remembrance.Card.Management
{
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
        private readonly IMessenger _messenger;

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
            [NotNull] IMessenger messenger,
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

        public TranslationInfo ReloadTranslationDetailsIfNeeded(TranslationEntry translationEntry)
        {
            if (_translationDetailsRepository.CheckByTranslationEntryId(translationEntry.Id))
                return new TranslationInfo(translationEntry, _translationDetailsRepository.GetByTranslationEntryId(translationEntry.Id));

            var translationResult = Translate(translationEntry.Key);
            var translationDetails = new TranslationDetails(translationResult, translationEntry.Id);
            _logger.Trace($"Saving translation details for {translationEntry.Key}...");
            _translationDetailsRepository.Save(translationDetails);

            return new TranslationInfo(translationEntry, translationDetails);
        }

        public bool ProcessNewWord(string word, string sourceLanguage, string targetLanguage, bool showCard)
        {
            _logger.Info($"Processing word {word}...");
            if (word == null)
                throw new ArgumentNullException(nameof(word));

            TranslationInfo translationInfo;
            try
            {
                translationInfo = AddWord(word, sourceLanguage, targetLanguage);
            }
            catch (LocalizableException ex)
            {
                _logger.Warn(ex.Message);
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                _logger.Warn($"Word {word} has not been processed");
                return false;
            }

            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                _cardManager.ShowCard(translationInfo);
            _logger.Trace($"Word {word} has been processed");
            return true;
        }

        public bool ChangeWord(object id, string text, string sourceLanguage, string targetLanguage, bool showCard)
        {
            _logger.Info($"Changing text for {text} for word {id}...");
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));
            if (targetLanguage == null)
                throw new ArgumentNullException(nameof(targetLanguage));

            TranslationInfo translationInfo;
            try
            {
                var key = GetTranslationKey(text, sourceLanguage, targetLanguage);
                var translationResult = Translate(key);
                //replace the original text with the corrected one
                key.Text = translationResult.PartOfSpeechTranslations.First().Text;
                if (_translationEntryRepository.TryGetByKey(key) != null)
                    throw new LocalizableException($"An item with the same key {key} already exists", Errors.WordIsPresent);

                var translationEntry = new TranslationEntry(key, translationResult.GetDefaultWords())
                {
                    Id = id
                };
                _translationEntryRepository.Save(translationEntry);
                var translationDetails = new TranslationDetails(translationResult, translationEntry.Id);
                _translationDetailsRepository.Save(translationDetails);
                translationInfo = new TranslationInfo(translationEntry, translationDetails);
            }
            catch (LocalizableException ex)
            {
                _logger.Warn(ex.Message);
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                _logger.Warn($"Text was not changed for word {id}");
                return false;
            }

            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                _cardManager.ShowCard(translationInfo);
            _logger.Trace($"Text has been changed for word {id}");
            return true;
        }

        public TranslationInfo AddWord(string text, string sourceLanguage, string targetLanguage)
        {
            _logger.Trace($"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");

            var key = GetTranslationKey(text, sourceLanguage, targetLanguage);
            var translationResult = Translate(key);
            //replace the original text with the corrected one
            key.Text = translationResult.PartOfSpeechTranslations.First().Text;
            var translationEntry = _translationEntryRepository.TryGetByKey(key) ?? new TranslationEntry(key, translationResult.GetDefaultWords());
            var id = _translationEntryRepository.Save(translationEntry);
            var translationDetails = new TranslationDetails(translationResult, id);
            _translationDetailsRepository.Save(translationDetails);
            _logger.Trace($"Translation for {key} has been successfully added");

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

        [NotNull]
        private TranslationEntryKey GetTranslationKey([CanBeNull] string text, string sourceLanguage, string targetLanguage)
        {
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

            return new TranslationEntryKey(text, sourceLanguage, targetLanguage);
        }

        [NotNull]
        private TranslationResult Translate([NotNull] TranslationEntryKey key)
        {
            //Used En as ui language to simplify conversion of common words to the enums
            var translationResult = _wordsTranslator.GetTranslationAsync(key.SourceLanguage, key.TargetLanguage, key.Text, Constants.EnLanguage).Result;
            if (!translationResult.PartOfSpeechTranslations.Any())
                throw new LocalizableException($"No translations found for {key}", Errors.CannotTranslate);
            return translationResult;
        }
    }
}