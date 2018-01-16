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
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
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
        private readonly IImageDownloader _imageDownloader;

        [NotNull]
        private readonly IImageSearcher _imageSearcher;

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IPredictor _predictor;

        [NotNull]
        private readonly IPrepositionsInfoRepository _prepositionsInfoRepository;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

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
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IPredictor predictor,
            [NotNull] IImageSearcher imageSearcher,
            [NotNull] IWordImagesInfoRepository imagesInfoRepository,
            [NotNull] IImageDownloader imageDownloader,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository)
        {
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _imageDownloader = imageDownloader ?? throw new ArgumentNullException(nameof(imageDownloader));
            _wordImagesInfoRepository = imagesInfoRepository ?? throw new ArgumentNullException(nameof(imagesInfoRepository));
            _imageSearcher = imageSearcher ?? throw new ArgumentNullException(nameof(imageSearcher));
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
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
            var translationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(id);
            if (translationDetails != null)
            {
                processNonReloaded?.Invoke(translationDetails);
                ReloadAdditionalInfoIfNeeded(text, translationDetails, cancellationToken);
                return translationDetails;
            }

            var translationResult = await TranslateAsync(text, sourceLanguage, targetLanguage, manualTranslations, cancellationToken)
                .ConfigureAwait(false);

            // There are no translation details for this word
            translationDetails = new TranslationDetails(translationResult, id);
            _translationDetailsRepository.Save(translationDetails);
            ReloadAdditionalInfoIfNeeded(text, translationDetails, cancellationToken);
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
                throw new InvalidOperationException();

            var translationResult = await TranslateAsync(key.Text, key.SourceLanguage, key.TargetLanguage, manualTranslations, cancellationToken)
                .ConfigureAwait(false);

            // replace the original text with the corrected one
            key.Text = translationResult.PartOfSpeechTranslations.First()
                .Text;
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
                Id = id,
                ManualTranslations = manualTranslations
            };

            id = _translationEntryRepository.Save(translationEntry);

            var existingTranslationDetails = _translationDetailsRepository.TryGetByTranslationEntryId(id);

            var translationDetails = new TranslationDetails(translationResult, id);
            if (existingTranslationDetails != null)
                translationDetails.Id = existingTranslationDetails.Id;

            _translationDetailsRepository.Save(translationDetails);
            ReloadAdditionalInfoIfNeeded(text, translationDetails, cancellationToken);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);

            _logger.Trace($"Translation for {key} has been successfully added");
            if (needPostProcess)
                await PostProcessWordAsync(ownerWindow, translationInfo, cancellationToken)
                    .ConfigureAwait(false);
            _logger.Trace($"Processing finished for word {text}");
            return translationInfo;
        }

        public async Task<string> GetDefaultTargetLanguageAsync(string sourceLanguage, CancellationToken cancellationToken)
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

            return await Task.FromResult(targetLanguage)
                .ConfigureAwait(false);
        }

        public async Task<TranslationInfo> UpdateManualTranslationsAsync(object id, ManualTranslation[] manualTranslations, CancellationToken cancellationToken)
        {
            var translationEntry = _translationEntryRepository.GetById(id);
            translationEntry.ManualTranslations = manualTranslations;
            _translationEntryRepository.Save(translationEntry);
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

            _translationDetailsRepository.Save(translationDetails);
            return new TranslationInfo(translationEntry, translationDetails);
        }

        private void ReloadAdditionalInfoIfNeeded([NotNull] string text, [NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            //Fire and Forget
#pragma warning disable 4014
            ReloadAdditionalInfoAsync(text, translationDetails, cancellationToken);
#pragma warning restore 4014
        }

        private async void ReloadAdditionalInfoAsync([NotNull] string text, [NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            await ReloadPrepositionsAsync(text, translationDetails, cancellationToken);
            await ReloadImagesAsync(translationDetails, cancellationToken);
        }

        private async Task ReloadPrepositionsAsync([NotNull] string text, [NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            if (_prepositionsInfoRepository.CheckPrepositionsInfoExists(translationDetails.TranslationEntryId))
                return;
            _logger.Info($"Reloading preposition for {translationDetails.TranslationEntryId}...");
            var prepositions = await GetPrepositionsCollectionAsync(text, cancellationToken)
                .ConfigureAwait(false);
            var prepositionsInfo = new PrepositionsInfo(translationDetails.TranslationEntryId, prepositions);
            _prepositionsInfoRepository.Save(prepositionsInfo);
            _messenger.Publish(prepositionsInfo);
        }

        private async Task ReloadImagesAsync([NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            //TODO https://api.qwant.com/api/search/images?count=1&offset=0&q=horse
            //http://www.faroo.com/hp/api/api.html#ratelimit

            //TODO: 1) Separate DBs for Preposition and Images.
            //2) sTORE 3 IMAGES WITH THUMBNAILS AND IMAGE Bitmaps in separate DB
            //3) When image or preposition is loaded- send event through messageHub. All forms which display image or prep should handle it.
            //4)Image control - like in PhotoReviewer - display square image with two arrows, cross to hide image at all and Browse to upload own image
            _logger.Info($"Reloading images for {translationDetails.TranslationEntryId}...");
            var translationEntry = _translationEntryRepository.GetById(translationDetails.TranslationEntryId);
            IList<Task> tasks = translationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(
                    partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants.Select(translationVariant => ReloadAndUpdateImageAsync(translationEntry, translationVariant, cancellationToken)))
                .ToList();
            await Task.WhenAll(tasks);
            translationDetails.ImagesUrlsAreLoaded = true;
        }

        private async Task ReloadAndUpdateImageAsync([NotNull] TranslationEntry translationEntry, [NotNull] TranslationVariant translationVariant, CancellationToken cancellationToken)
        {
            if (_wordImagesInfoRepository.CheckImagesInfoExists(translationEntry.Id, translationVariant))
                return;

            var imagesUrls = await _imageSearcher.SearchImagesAsync(translationVariant.Text, translationEntry.Key.TargetLanguage, cancellationToken, 0, 3)
                .ConfigureAwait(false);
            if (imagesUrls == null)
                return;

            var tasks = new List<Task<byte[]>>(imagesUrls.Length * 2);
            foreach (var image in imagesUrls)
            {
                tasks.Add(_imageDownloader.DownloadImageAsync(image.Url));
                tasks.Add(_imageDownloader.DownloadImageAsync(image.ThumbnailUrl));
            }

            var images = await Task.WhenAll(tasks)
                .ConfigureAwait(false);
            var i = 0;
            //The order of images should be correct after Task.WhenAll
            var imagesWithBitmap = imagesUrls.Select(
                    image => new ImageInfoWithBitmap
                    {
                        ImageBitmap = images[i++],
                        ThumbnailBitmap = images[i++],
                        ImageInfo = image
                    })
                .ToArray();

            var wordImagesInfo = new WordImagesInfo(translationEntry.Id, translationVariant.Text, translationVariant.PartOfSpeech, imagesWithBitmap);
            _wordImagesInfoRepository.Save(wordImagesInfo);
            _messenger.Publish(wordImagesInfo);
        }

        [ItemNotNull]
        private async Task<PrepositionsCollection> GetPrepositionsCollectionAsync([NotNull] string text, CancellationToken cancellationToken)
        {
            var predictionResult = await _predictor.PredictAsync(text, 5, cancellationToken)
                .ConfigureAwait(false);
            var prepositionsCollection = new PrepositionsCollection
            {
                Texts = predictionResult.Position > 0
                    ? predictionResult.PredictionVariants
                    : null
            };
            return prepositionsCollection;
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
                deletedManualTranslations = deletedManualTranslations.Except(manualTranslations, _wordsEqualityComparer);
            foreach (var word in deletedManualTranslations)
                _wordPriorityRepository.MarkNonPriority(word, id);
        }

        [ItemNotNull]
        private async Task<TranslationEntryKey> GetTranslationKeyAsync([CanBeNull] string text, [CanBeNull] string sourceLanguage, [CanBeNull] string targetLanguage, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new LocalizableException("Text is empty", Errors.WordIsMissing);

            if (sourceLanguage == null || sourceLanguage == Constants.AutoDetectLanguage)
            {
                // if not specified or autodetect - try to detect
                var detectionResult = await _languageDetector.DetectLanguageAsync(text, cancellationToken)
                    .ConfigureAwait(false);
                sourceLanguage = detectionResult.Language;
            }

            if (sourceLanguage == null)
                throw new LocalizableException($"Cannot detect language for '{text}'", Errors.CannotDetectLanguage);

            if (targetLanguage == null || targetLanguage == Constants.AutoDetectLanguage)

                // if not specified - try to find best matching target language
                targetLanguage = await GetDefaultTargetLanguageAsync(sourceLanguage, cancellationToken)
                    .ConfigureAwait(false);

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
                    throw new LocalizableException($"No translations found for {text}", string.Format(Errors.CannotTranslate, text, sourceLanguage, targetLanguage));
            }
            else
            {
                _logger.Trace("Received translation");
            }

            if (manualTranslations != null)
                translationResult.PartOfSpeechTranslations = ConcatTranslationsWithManual(text, manualTranslations, translationResult.PartOfSpeechTranslations)
                    .ToArray();

            return translationResult;
        }
    }
}