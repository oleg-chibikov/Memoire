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
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] IWordImagesInfoRepository wordImagesInfoRepository,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository)
        {
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _wordImagesInfoRepository = wordImagesInfoRepository ?? throw new ArgumentNullException(nameof(wordImagesInfoRepository));
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
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
            ICollection<ManualTranslation> manualTranslations,
            CancellationToken cancellationToken,
            Action<TranslationDetails> processNonReloaded)
        {
            if (!manualTranslations?.Any() == true)
            {
                manualTranslations = null;
            }
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
            _translationEntryRepository.Delete(translationEntryKey);
        }

        public async Task<TranslationInfo> AddOrUpdateTranslationEntryAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            CancellationToken cancellationToken,
            IWindow ownerWindow,
            bool needPostProcess,
            ICollection<ManualTranslation> manualTranslations)
        {
            _logger.TraceFormat("Adding new word translation for {0}...", translationEntryAdditionInfo);
            if (translationEntryAdditionInfo == null)
            {
                throw new ArgumentNullException(nameof(translationEntryAdditionInfo));
            }

            if (!manualTranslations?.Any() == true)
            {
                manualTranslations = null;
            }

            // This method replaces translation with the actual one
            var translationEntryKey = await GetTranslationKeyAsync(translationEntryAdditionInfo, cancellationToken).ConfigureAwait(false);
            if (translationEntryKey.Text == null)
            {
                throw new InvalidOperationException();
            }

            var translationResult = await TranslateAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);

            // replace the original text with the corrected one
            translationEntryKey.Text = translationResult.PartOfSpeechTranslations.First().Text;

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
                //no await here
                PostProcessWordAsync(ownerWindow, translationInfo, cancellationToken).ConfigureAwait(false);
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

        public async Task<TranslationInfo> UpdateManualTranslationsAsync(TranslationEntryKey translationEntryKey, ICollection<ManualTranslation> manualTranslations, CancellationToken cancellationToken)
        {
            _logger.TraceFormat("Updating manual translations for {0}...", translationEntryKey);
            if (translationEntryKey == null)
            {
                throw new ArgumentNullException(nameof(translationEntryKey));
            }

            if (!manualTranslations?.Any() == true)
            {
                manualTranslations = null;
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
            DeleteFromPriority(translationEntry, manualTranslations, translationDetails);
            return new TranslationInfo(translationEntry, translationDetails);
        }

        [NotNull]
        private static IEnumerable<PartOfSpeechTranslation> ConcatTranslationsWithManual(
            [NotNull] string text,
            [NotNull] ICollection<ManualTranslation> manualTranslations,
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

        private void DeleteFromPriority([NotNull] TranslationEntry translationEntry, [CanBeNull] ICollection<ManualTranslation> manualTranslations, [NotNull] TranslationDetails translationDetails)
        {
            var remainingManualTranslations = translationDetails.TranslationResult.PartOfSpeechTranslations.Where(x => x.IsManual)
                .SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants)
                .Cast<BaseWord>();
            var deletedManualTranslations = remainingManualTranslations;
            if (manualTranslations != null)
            {
                deletedManualTranslations = deletedManualTranslations.Except(manualTranslations);
            }

            if (translationEntry.PriorityWords == null)
            {
                return;
            }
            var deleted = false;
            foreach (var word in deletedManualTranslations)
            {
                translationEntry.PriorityWords.Remove(word);
                deleted = true;
            }

            if (deleted)
            {
                _translationEntryRepository.Update(translationEntry);
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
                throw new LocalizableException(Errors.WordIsMissing, "Text is empty");
            }

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
            {
                // if not specified or autodetect - try to detect
                var detectionResult = await _languageDetector.DetectLanguageAsync(text, cancellationToken).ConfigureAwait(false);
                sourceLanguage = detectionResult.Language;
            }

            if (sourceLanguage == null)
            {
                throw new LocalizableException(Errors.CannotDetectLanguage, $"Cannot detect language for '{text}'");
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
        private async Task<TranslationResult> TranslateAsync([NotNull] TranslationEntryKey translationEntryKey, [CanBeNull] ICollection<ManualTranslation> manualTranslations, CancellationToken cancellationToken)
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
                        throw new InvalidOperationException("No translations and manual translations are not set");
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
                throw new LocalizableException(string.Format(Errors.CannotTranslate, translationEntryKey), ex, $"Cannot translate {translationEntryKey}");
            }
        }
    }
}