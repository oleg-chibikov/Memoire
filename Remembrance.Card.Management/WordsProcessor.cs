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
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Card.Management
{
    //TODO: Think about class interface - it is not clear now
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
            var translationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(translationEntry.Id);
            if (translationDetails != null)
                return new TranslationInfo(translationEntry, translationDetails);

            var translationResult = Translate(translationEntry.Key);
            //There are no translation details for this word
            translationDetails = new TranslationDetails(translationResult, translationEntry.Id);
            _logger.Trace($"Saving translation details for {translationEntry.Key}...");
            _translationDetailsRepository.Save(translationDetails);

            return new TranslationInfo(translationEntry, translationDetails);
        }

        public bool ProcessNewWord(string text, string sourceLanguage, string targetLanguage, IWindow ownerWindow)
        {
            _logger.Info($"Processing word {text}...");
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            return ProcessWordInternal(null, text, sourceLanguage, targetLanguage, ownerWindow);
        }

        public bool ChangeWord(object id, string text, string sourceLanguage, string targetLanguage, IWindow ownerWindow)
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

            return ProcessWordInternal(id, text, sourceLanguage, targetLanguage, ownerWindow);
        }

        public TranslationInfo AddWord(string text, string sourceLanguage, string targetLanguage, object id)
        {
            //This method replaces translation with the actual one
            _logger.Trace($"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");

            var key = GetTranslationKey(text, sourceLanguage, targetLanguage);
            var translationResult = Translate(key);
            //replace the original text with the corrected one
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

            var translationEntry = new TranslationEntry(key, translationResult.GetDefaultWords())
            {
                Id = id
            };

            id = _translationEntryRepository.Save(translationEntry);

            var existingTranslationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(id);

            var translationDetails = new TranslationDetails(translationResult, id);
            if (existingTranslationDetails != null)
                translationDetails.Id = existingTranslationDetails.Id;

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

        private void PostProcessWord(IWindow ownerWindow, [NotNull] TranslationInfo translationInfo)
        {
            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);
            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            _cardManager.ShowCard(translationInfo, ownerWindow);
        }

        private bool ProcessWordInternal(object id, string text, string sourceLanguage, string targetLanguage, IWindow ownerWindow)
        {
            TranslationInfo translationInfo;
            try
            {
                translationInfo = AddWord(text, sourceLanguage, targetLanguage, id);
            }
            catch (LocalizableException ex)
            {
                _logger.Warn(ex.Message);
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                _logger.Warn($"Processing failed for word {text}");
                return false;
            }

            PostProcessWord(ownerWindow, translationInfo);
            _logger.Trace($"Processing finished for word {text}");
            return true;
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