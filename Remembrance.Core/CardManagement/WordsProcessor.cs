using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
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
    [UsedImplicitly]
    internal sealed class WordsProcessor : IWordsProcessor
    {
        private static readonly string CurerntCultureLanguage = CultureUtilities.GetCurrentCulture()
            .TwoLetterISOLanguageName;

        private static readonly string[] DefaultTargetLanguages =
        {
            Constants.EnLanguageTwoLetters,
            CurerntCultureLanguage == Constants.EnLanguageTwoLetters
                ? Constants.RuLanguageTwoLetters
                : CurerntCultureLanguage
        };

        [NotNull]
        private readonly ITranslationDetailsCardManager _cardManager;

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
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        private readonly IWordsTranslator _wordsTranslator;

        public WordsProcessor(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ILog logger,
            [NotNull] ITranslationDetailsCardManager cardManager,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IWordsTranslator wordsTranslator,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer)
        {
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
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

        public async Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            object id,
            string text,
            string sourceLanguage,
            string targetLanguage,
            ManualTranslation[] manualTranslations,
            CancellationToken cancellationToken,
            Action<TranslationDetails> processNonReloaded)
        {
            var translationDetails = _translationDetailsRepository.TryGetById(id);
            if (translationDetails != null)
            {
                processNonReloaded?.Invoke(translationDetails);
                return translationDetails;
            }

            var translationResult = await TranslateAsync(text, sourceLanguage, targetLanguage, manualTranslations, cancellationToken)
                .ConfigureAwait(false);

            // There are no translation details for this word
            translationDetails = new TranslationDetails(translationResult, id);
            _translationDetailsRepository.Insert(translationDetails);
            return translationDetails;
        }

        public async Task<TranslationInfo> AddOrChangeWordAsync(
            string text,
            CancellationToken cancellationToken,
            string sourceLanguage,
            string targetLanguage,
            IWindow ownerWindow,
            bool needPostProcess,
            object id,
            ManualTranslation[] manualTranslations)
        {
            // This method replaces translation with the actual one
            _logger.Info(
                id != null
                    ? $"Changing word translation ({id}) to {text} ({sourceLanguage} - {targetLanguage})..."
                    : $"Adding new word translation for {text} ({sourceLanguage} - {targetLanguage})...");

            var key = await GetTranslationKeyAsync(text, sourceLanguage, targetLanguage, cancellationToken)
                .ConfigureAwait(false);
            if (text == null)
            {
                throw new InvalidOperationException();
            }

            var translationResult = await TranslateAsync(key.Text, key.SourceLanguage, key.TargetLanguage, manualTranslations, cancellationToken)
                .ConfigureAwait(false);

            // replace the original text with the corrected one
            key.Text = translationResult.PartOfSpeechTranslations.First()
                .Text;
            var existingByKey = _translationEntryRepository.TryGetByKey(key);
            if (id != null)
            {
                if (!existingByKey?.Id.Equals(id) == true)
                {
                    throw new LocalizableException($"An item with the same key {key} already exists", Errors.WordIsPresent);
                }
            }
            else
            {
                if (existingByKey != null)
                {
                    id = existingByKey.Id;
                }
            }

            var translationEntry = new TranslationEntry(key)
            {
                Id = id,
                ManualTranslations = manualTranslations
            };

            id = _translationEntryRepository.Insert(translationEntry);

            var existingTranslationDetails = _translationDetailsRepository.TryGetById(id);

            var translationDetails = new TranslationDetails(translationResult, id);
            if (existingTranslationDetails != null)
            {
                translationDetails.Id = existingTranslationDetails.Id;
            }

            _translationDetailsRepository.Insert(translationDetails);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);

            _logger.Trace($"Translation for {key} has been successfully added");
            if (needPostProcess)
            {
                await PostProcessWordAsync(ownerWindow, translationInfo, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.Trace($"Processing finished for word {text}");
            return translationInfo;
        }

        public async Task<string> GetDefaultTargetLanguageAsync(string sourceLanguage, CancellationToken cancellationToken)
        {
            if (sourceLanguage == null)
            {
                throw new ArgumentNullException(nameof(sourceLanguage));
            }

            var settings = _settingsRepository.Get();
            var lastUsedTargetLanguage = settings.LastUsedTargetLanguage;
            var possibleTargetLanguages = new Stack<string>(DefaultTargetLanguages);
            if (lastUsedTargetLanguage != null && lastUsedTargetLanguage != Constants.AutoDetectLanguage)
            {
                possibleTargetLanguages.Push(lastUsedTargetLanguage); // top priority
            }

            var targetLanguage = possibleTargetLanguages.Pop();
            while (targetLanguage == sourceLanguage)
            {
                targetLanguage = possibleTargetLanguages.Pop();
            }

            return await Task.FromResult(targetLanguage)
                .ConfigureAwait(false);
        }

        public async Task<TranslationInfo> UpdateManualTranslationsAsync(object id, ManualTranslation[] manualTranslations, CancellationToken cancellationToken)
        {
            var translationEntry = _translationEntryRepository.GetById(id);
            translationEntry.ManualTranslations = manualTranslations;
            _translationEntryRepository.Update(translationEntry);
            var key = translationEntry.Key;
            var translationDetails = await ReloadTranslationDetailsIfNeededAsync(
                    id,
                    key.Text,
                    key.SourceLanguage,
                    key.TargetLanguage,
                    manualTranslations,
                    cancellationToken,
                    td =>
                    {
                        //if the new ones were applied - no need to merge
                        var nonManual = td.TranslationResult.PartOfSpeechTranslations.Where(x => !x.IsManual);
                        td.TranslationResult.PartOfSpeechTranslations = (manualTranslations != null
                            ? ConcatTranslationsWithManual(key.Text, manualTranslations, nonManual)
                            : nonManual).ToArray();
                    })
                .ConfigureAwait(false);
            DeleteFromPriority(id, manualTranslations, translationDetails);
            return new TranslationInfo(translationEntry, translationDetails);
        }

        [NotNull]
        private static IEnumerable<PartOfSpeechTranslation> ConcatTranslationsWithManual(
            [NotNull] string text,
            [NotNull] ManualTranslation[] manualTranslations,
            [NotNull] IEnumerable<PartOfSpeechTranslation> partOfSpeechTranslations)
        {
            var groups = manualTranslations.GroupBy(x => x.PartOfSpeech);
            var manualPartOfSpeechTranslations = groups.Select(
                group => new PartOfSpeechTranslation
                {
                    IsManual = true,
                    PartOfSpeech = group.Key,
                    Text = text,
                    TranslationVariants = group.Select(
                            manualTranslation => new TranslationVariant
                            {
                                Text = manualTranslation.Text,
                                PartOfSpeech = manualTranslation.PartOfSpeech,
                                Examples = string.IsNullOrWhiteSpace(manualTranslation.Example)
                                    ? null
                                    : new[]
                                    {
                                        new Example
                                        {
                                            Text = manualTranslation.Example
                                        }
                                    },
                                Meanings = string.IsNullOrWhiteSpace(manualTranslation.Meaning)
                                    ? null
                                    : new[]
                                    {
                                        new Word
                                        {
                                            Text = manualTranslation.Meaning
                                        }
                                    }
                            })
                        .ToArray()
                });
            return partOfSpeechTranslations.Concat(manualPartOfSpeechTranslations);
        }

        private void DeleteFromPriority([NotNull] object id, [CanBeNull] ManualTranslation[] manualTranslations, [NotNull] TranslationDetails translationDetails)
        {
            var remainingManualTranslations = translationDetails.TranslationResult.PartOfSpeechTranslations.Where(x => x.IsManual)
                .SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants)
                .Cast<IWord>();
            var deletedManualTranslations = remainingManualTranslations;
            if (manualTranslations != null)
            {
                deletedManualTranslations = deletedManualTranslations.Except(manualTranslations, _wordsEqualityComparer);
            }

            foreach (var word in deletedManualTranslations)
            {
                _wordPriorityRepository.MarkNonPriority(word, id);
            }
        }

        [ItemNotNull]
        private async Task<TranslationEntryKey> GetTranslationKeyAsync([CanBeNull] string text, [CanBeNull] string sourceLanguage, [CanBeNull] string targetLanguage, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);
            }

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
            {
                // if not specified or autodetect - try to detect
                var detectionResult = await _languageDetector.DetectLanguageAsync(text, cancellationToken)
                    .ConfigureAwait(false);
                sourceLanguage = detectionResult.Language;
            }

            if (sourceLanguage == null)
            {
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);
            }

            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)

                // if not specified - try to find best matching target language
            {
                targetLanguage = await GetDefaultTargetLanguageAsync(sourceLanguage, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new TranslationEntryKey(text, sourceLanguage, targetLanguage);
        }

        private async Task PostProcessWordAsync([CanBeNull] IWindow ownerWindow, [NotNull] TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            _messenger.Publish(translationInfo);
            _cardManager.ShowCard(translationInfo, ownerWindow);
            await _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage, cancellationToken)
                .ConfigureAwait(false);
        }

        [ItemNotNull]
        private async Task<TranslationResult> TranslateAsync(
            [NotNull] string text,
            [NotNull] string sourceLanguage,
            [NotNull] string targetLanguage,
            [CanBeNull] ManualTranslation[] manualTranslations,
            CancellationToken cancellationToken)
        {
            // Used En as ui language to simplify conversion of common words to the enums
            var translationResult = await _wordsTranslator.GetTranslationAsync(sourceLanguage, targetLanguage, text, Constants.EnLanguage, cancellationToken)
                .ConfigureAwait(false);
            if (!translationResult.PartOfSpeechTranslations.Any())
            {
                _logger.Warn($"No translations found for {text}");

                if (manualTranslations == null)
                {
                    throw new LocalizableException($"No translations found for {text}", string.Format(Errors.CannotTranslate, text, sourceLanguage, targetLanguage));
                }
            }
            else
            {
                _logger.Trace("Received translation");
            }

            if (manualTranslations != null)
            {
                translationResult.PartOfSpeechTranslations = ConcatTranslationsWithManual(text, manualTranslations, translationResult.PartOfSpeechTranslations)
                    .ToArray();
            }

            return translationResult;
        }
    }
}