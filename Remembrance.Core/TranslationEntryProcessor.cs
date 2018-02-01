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
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core
{
    [UsedImplicitly]
    internal sealed class TranslationEntryProcessor : ITranslationEntryProcessor
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
        private readonly ITranslationDetailsCardManager _cardManager;

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IPrepositionsInfoRepository _prepositionsInfoRepository;

        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordImagesInfoRepository _wordImagesInfoRepository;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        private readonly IWordsTranslator _wordsTranslator;

        public TranslationEntryProcessor(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ILog logger,
            [NotNull] ITranslationDetailsCardManager cardManager,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IWordsTranslator wordsTranslator,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] IWordImagesInfoRepository wordImagesInfoRepository,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository)
        {
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _wordImagesInfoRepository = wordImagesInfoRepository ?? throw new ArgumentNullException(nameof(wordImagesInfoRepository));
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _wordsTranslator = wordsTranslator ?? throw new ArgumentNullException(nameof(wordsTranslator));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cardManager = cardManager ?? throw new ArgumentNullException(nameof(cardManager));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public async Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            TranslationEntryKey translationEntryKey,
            ManualTranslation[] manualTranslations,
            CancellationToken cancellationToken,
            Action<TranslationDetails> processNonReloaded)
        {
            var translationDetails = _translationDetailsRepository.TryGetById(translationEntryKey);
            if (translationDetails != null)
            {
                processNonReloaded?.Invoke(translationDetails);
                return translationDetails;
            }

            var translationResult = await TranslateAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);

            // There are no translation details for this word
            translationDetails = new TranslationDetails(translationResult, translationEntryKey);
            _translationDetailsRepository.Insert(translationDetails);
            return translationDetails;
        }

        public void DeleteTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            if (translationEntryKey == null)
            {
                throw new ArgumentNullException(nameof(translationEntryKey));
            }

            _prepositionsInfoRepository.Delete(translationEntryKey);
            _translationDetailsRepository.Delete(translationEntryKey);
            _wordImagesInfoRepository.ClearForTranslationEntry(translationEntryKey);
            _wordPriorityRepository.ClearForTranslationEntry(translationEntryKey);
            _translationEntryRepository.Delete(translationEntryKey);
        }

        public async Task<TranslationInfo> AddOrUpdateTranslationEntryAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            CancellationToken cancellationToken,
            IWindow ownerWindow,
            bool needPostProcess,
            ManualTranslation[] manualTranslations)
        {
            if (translationEntryAdditionInfo == null)
            {
                throw new ArgumentNullException(nameof(translationEntryAdditionInfo));
            }

            // This method replaces translation with the actual one
            _logger.TraceFormat("Adding new word translation for {0}...", translationEntryAdditionInfo);

            var translationEntryKey = await GetTranslationKeyAsync(translationEntryAdditionInfo, cancellationToken).ConfigureAwait(false);
            if (translationEntryKey.Text == null)
            {
                throw new InvalidOperationException();
            }

            var translationResult = await TranslateAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);

            // replace the original text with the corrected one
            translationEntryKey.Text = translationResult.PartOfSpeechTranslations.First().WordText;

            var translationEntry = new TranslationEntry(translationEntryKey)
            {
                Id = translationEntryKey,
                ManualTranslations = manualTranslations
            };
            _translationEntryRepository.Upsert(translationEntry);
            var translationDetails = new TranslationDetails(translationResult, translationEntryKey);
            _translationDetailsRepository.Upsert(translationDetails);

            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
            if (needPostProcess)
            {
                PostProcessWordAsync(ownerWindow, translationInfo, cancellationToken);
            }

            _logger.InfoFormat("Processing finished for word {0}", translationEntryKey);
            return translationInfo;
        }

        public async Task<string> GetDefaultTargetLanguageAsync(string sourceLanguage, CancellationToken cancellationToken)
        {
            if (sourceLanguage == null)
            {
                throw new ArgumentNullException(nameof(sourceLanguage));
            }

            var settings = _localSettingsRepository.Get();
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

            return await Task.FromResult(targetLanguage).ConfigureAwait(false);
        }

        public async Task<TranslationInfo> UpdateManualTranslationsAsync(TranslationEntryKey translationEntryKey, ManualTranslation[] manualTranslations, CancellationToken cancellationToken)
        {
            if (translationEntryKey == null)
            {
                throw new ArgumentNullException(nameof(translationEntryKey));
            }

            var translationEntry = _translationEntryRepository.GetById(translationEntryKey);
            translationEntry.ManualTranslations = manualTranslations;
            _translationEntryRepository.Update(translationEntry);
            var translationDetails = await ReloadTranslationDetailsIfNeededAsync(
                    translationEntryKey,
                    manualTranslations,
                    cancellationToken,
                    td =>
                    {
                        //if the new ones were applied - no need to merge
                        var nonManual = td.TranslationResult.PartOfSpeechTranslations.Where(x => !x.IsManual);
                        td.TranslationResult.PartOfSpeechTranslations = (manualTranslations != null
                            ? ConcatTranslationsWithManual(translationEntryKey.Text, manualTranslations, nonManual)
                            : nonManual).ToArray();
                        _translationDetailsRepository.Update(td);
                    })
                .ConfigureAwait(false);
            DeleteFromPriority(translationEntryKey, manualTranslations, translationDetails);
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
                    WordText = text,
                    TranslationVariants = group.Select(
                            manualTranslation => new TranslationVariant
                            {
                                WordText = manualTranslation.WordText,
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
                                            WordText = manualTranslation.Meaning
                                        }
                                    }
                            })
                        .ToArray()
                });
            return partOfSpeechTranslations.Concat(manualPartOfSpeechTranslations);
        }

        private void DeleteFromPriority([NotNull] TranslationEntryKey translationEntryKey, [CanBeNull] ManualTranslation[] manualTranslations, [NotNull] TranslationDetails translationDetails)
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
                _wordPriorityRepository.Delete(new WordKey(translationEntryKey, word));
            }
        }

        [ItemNotNull]
        private async Task<TranslationEntryKey> GetTranslationKeyAsync([NotNull] TranslationEntryAdditionInfo translationEntryAdditionInfo, CancellationToken cancellationToken)
        {
            if (translationEntryAdditionInfo == null)
            {
                throw new ArgumentNullException(nameof(translationEntryAdditionInfo));
            }

            var text = translationEntryAdditionInfo.Text;
            var sourceLanguage = translationEntryAdditionInfo.SourceLanguage;
            var targetLanguage = translationEntryAdditionInfo.TargetLanguage;
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);
            }

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
            {
                // if not specified or autodetect - try to detect
                var detectionResult = await _languageDetector.DetectLanguageAsync(text, cancellationToken).ConfigureAwait(false);
                sourceLanguage = detectionResult.Language;
            }

            if (sourceLanguage == null)
            {
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);
            }

            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)

                // if not specified - try to find best matching target language
            {
                targetLanguage = await GetDefaultTargetLanguageAsync(sourceLanguage, cancellationToken).ConfigureAwait(false);
            }

            return new TranslationEntryKey(text, sourceLanguage, targetLanguage);
        }

        private async Task PostProcessWordAsync([CanBeNull] IWindow ownerWindow, [NotNull] TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            _messenger.Publish(translationInfo);
            _cardManager.ShowCard(translationInfo, ownerWindow);
            await _textToSpeechPlayer.PlayTtsAsync(translationInfo.TranslationEntryKey.Text, translationInfo.TranslationEntryKey.SourceLanguage, cancellationToken).ConfigureAwait(false);
        }

        [ItemNotNull]
        private async Task<TranslationResult> TranslateAsync([NotNull] TranslationEntryKey translationEntryKey, [CanBeNull] ManualTranslation[] manualTranslations, CancellationToken cancellationToken)
        {
            try
            {
                // Used En as ui language to simplify conversion of common words to the enums
                var translationResult = await _wordsTranslator.GetTranslationAsync(
                        translationEntryKey.SourceLanguage,
                        translationEntryKey.TargetLanguage,
                        translationEntryKey.Text,
                        Constants.EnLanguage,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (translationResult.PartOfSpeechTranslations.Any())
                {
                    _logger.TraceFormat("Received translation for {0}", translationEntryKey);
                }
                else
                {
                    _logger.WarnFormat("No translations found for {0}", translationEntryKey);

                    if (manualTranslations == null)
                    {
                        throw new LocalizableException($"No translations found for {translationEntryKey}", string.Format(Errors.CannotTranslate, translationEntryKey));
                    }
                }

                if (manualTranslations != null)
                {
                    translationResult.PartOfSpeechTranslations = ConcatTranslationsWithManual(translationEntryKey.Text, manualTranslations, translationResult.PartOfSpeechTranslations).ToArray();
                }

                return translationResult;
            }
            catch (Exception ex)
            {
                throw new LocalizableException($"Cannot translate {translationEntryKey}", ex, string.Format(Errors.CannotTranslate, translationEntryKey));
            }
        }
    }
}