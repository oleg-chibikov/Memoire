using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.Classification;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View.Settings;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using Scar.Common;
using Scar.Common.Localization;
using Scar.Common.Messages;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.ExtendedTranslation;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Core.Processing
{
    public sealed class TranslationEntryProcessor : ITranslationEntryProcessor
    {
        readonly ITranslationDetailsCardManager _cardManager;
        readonly ICultureManager _cultureManager;
        readonly ILanguageManager _languageManager;
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly IWindowDisplayer _windowDisplayer;
        readonly Func<ILoadingWindow> _loadingWindowFactory;
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly IPrepositionsInfoRepository _prepositionsInfoRepository;
        readonly ITextToSpeechPlayerWrapper _textToSpeechPlayerWrapper;
        readonly ITranslationDetailsRepository _translationDetailsRepository;
        readonly ITranslationEntryDeletionRepository _translationEntryDeletionRepository;
        readonly ITranslationEntryRepository _translationEntryRepository;
        readonly IWordImageInfoRepository _wordImageInfoRepository;
        readonly IWordImageSearchIndexRepository _wordImageSearchIndexRepository;
        readonly IWordsTranslator _wordsTranslator;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ISharedSettingsRepository _sharedSettingsRepository;
        readonly ILearningInfoCategoriesUpdater _learningInfoCategoriesUpdater;

        public TranslationEntryProcessor(
            ITextToSpeechPlayerWrapper textToSpeechPlayerWrapper,
            ILogger<TranslationEntryProcessor> logger,
            ITranslationDetailsCardManager cardManager,
            IMessageHub messageHub,
            ITranslationDetailsRepository translationDetailsRepository,
            IWordsTranslator wordsTranslator,
            ITranslationEntryRepository translationEntryRepository,
            IWordImageInfoRepository wordImageInfoRepository,
            IPrepositionsInfoRepository prepositionsInfoRepository,
            ILearningInfoRepository learningInfoRepository,
            ITranslationEntryDeletionRepository translationEntryDeletionRepository,
            IWordImageSearchIndexRepository wordImageSearchIndexRepository,
            ILanguageManager languageManager,
            Func<ILoadingWindow> loadingWindowFactory,
            ICultureManager cultureManager,
            ILocalSettingsRepository localSettingsRepository,
            ILearningInfoCategoriesUpdater learningInfoCategoriesUpdater,
            ISharedSettingsRepository sharedSettingsRepository,
            IWindowDisplayer windowDisplayer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _wordImageInfoRepository = wordImageInfoRepository ?? throw new ArgumentNullException(nameof(wordImageInfoRepository));
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryDeletionRepository = translationEntryDeletionRepository ?? throw new ArgumentNullException(nameof(translationEntryDeletionRepository));
            _wordImageSearchIndexRepository = wordImageSearchIndexRepository ?? throw new ArgumentNullException(nameof(wordImageSearchIndexRepository));
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _loadingWindowFactory = loadingWindowFactory ?? throw new ArgumentNullException(nameof(loadingWindowFactory));
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _learningInfoCategoriesUpdater = learningInfoCategoriesUpdater ?? throw new ArgumentNullException(nameof(learningInfoCategoriesUpdater));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _windowDisplayer = windowDisplayer ?? throw new ArgumentNullException(nameof(windowDisplayer));
            _wordsTranslator = wordsTranslator ?? throw new ArgumentNullException(nameof(wordsTranslator));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _textToSpeechPlayerWrapper = textToSpeechPlayerWrapper ?? throw new ArgumentNullException(nameof(textToSpeechPlayerWrapper));
            _cardManager = cardManager ?? throw new ArgumentNullException(nameof(cardManager));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public async Task<TranslationInfo?> AddOrUpdateTranslationEntryAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            IDisplayable? ownerWindow,
            bool needPostProcess,
            bool showLoader,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() => AddOrUpdateTranslationEntryInternalAsync(translationEntryAdditionInfo, ownerWindow, needPostProcess, showLoader, manualTranslations, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }

        public void DeleteTranslationEntry(TranslationEntryKey translationEntryKey, bool needDeletionRecord)
        {
            _ = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            _logger.LogTrace("Deleting {TranslationKey}{NeedDeletionRecord}...", translationEntryKey, needDeletionRecord ? " with creating deletion event" : null);
            _prepositionsInfoRepository.Delete(translationEntryKey);
            _translationDetailsRepository.Delete(translationEntryKey);
            _wordImageInfoRepository.ClearForTranslationEntry(translationEntryKey);
            _wordImageSearchIndexRepository.ClearForTranslationEntry(translationEntryKey);
            _translationEntryRepository.Delete(translationEntryKey);
            _learningInfoRepository.Delete(translationEntryKey);
            if (needDeletionRecord)
            {
                _translationEntryDeletionRepository.Upsert(new TranslationEntryDeletion(translationEntryKey));
            }

            _logger.LogInformation("Deleted {TranslationKey}", translationEntryKey);
        }

        public async Task<TranslationDetails> ReloadTranslationDetailsIfNeededAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            return (await ReloadTranslationDetailsIfNeededInternalAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false)).TranslationDetails;
        }

        public async Task<TranslationInfo> UpdateManualTranslationsAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Updating manual translations for {TranslationKey}...", translationEntryKey);
            _ = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            if (!manualTranslations?.Any() == true)
            {
                manualTranslations = null;
            }

            CapitalizeManualTranslations(manualTranslations);

            var translationEntry = _translationEntryRepository.GetById(translationEntryKey);
            translationEntry.ManualTranslations = manualTranslations;
            _translationEntryRepository.Update(translationEntry);

            var reloadResult = await ReloadTranslationDetailsIfNeededInternalAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);
            var translationDetails = reloadResult.TranslationDetails;

            DeleteFromPriority(translationEntry, manualTranslations, translationDetails);

            if (reloadResult.AlreadyExists)
            {
                var nonManual = translationDetails.TranslationResult.PartOfSpeechTranslations.Where(x => !x.IsManual);
                translationDetails.TranslationResult.PartOfSpeechTranslations =
                    (manualTranslations != null ? ConcatTranslationsWithManual(translationEntryKey.Text, manualTranslations, nonManual) : nonManual).ToArray();
                _translationDetailsRepository.Update(translationDetails);
            }

            var learningInfo = _learningInfoRepository.GetOrInsert(translationEntryKey);
            return new TranslationInfo(translationEntry, translationDetails, learningInfo);
        }

        static void CapitalizeManualTranslations(IReadOnlyCollection<ManualTranslation>? manualTranslations)
        {
            if (manualTranslations == null)
            {
                return;
            }

            foreach (var manualTranslation in manualTranslations)
            {
                manualTranslation.Text = manualTranslation.Text.Capitalize();
                manualTranslation.Example = manualTranslation.Example.Capitalize();
                manualTranslation.Meaning = manualTranslation.Meaning.Capitalize();
            }
        }

        static IEnumerable<PartOfSpeechTranslation> ConcatTranslationsWithManual(
            string text,
            IEnumerable<ManualTranslation> manualTranslations,
            IEnumerable<PartOfSpeechTranslation> partOfSpeechTranslations)
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
                                        new Example { Text = manualTranslation.Example }
                                    },
                                Meanings = string.IsNullOrWhiteSpace(manualTranslation.Meaning)
                                    ? null
                                    : new[]
                                    {
                                        new Word { Text = manualTranslation.Meaning }
                                    }
                            })
                        .ToArray()
                });
            return partOfSpeechTranslations.Concat(manualPartOfSpeechTranslations);
        }

        async Task<TranslationInfo?> AddOrUpdateTranslationEntryInternalAsync(
            TranslationEntryAdditionInfo translationEntryAdditionInfo,
            IDisplayable? ownerWindow,
            bool needPostProcess,
            bool showLoader,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            _cultureManager.ChangeCulture(CultureInfo.GetCultureInfo(_localSettingsRepository.UiLanguage));
            _ = translationEntryAdditionInfo ?? throw new ArgumentNullException(nameof(translationEntryAdditionInfo));
            _logger.LogTrace("Adding new word translation for {TranslationAdditionalInfo}...", translationEntryAdditionInfo);

            var executeInLoadingWindowContext =
                showLoader ? _windowDisplayer.DisplayWindow(_loadingWindowFactory) : null;

            try
            {
                if (!manualTranslations?.Any() == true)
                {
                    manualTranslations = null;
                }

                CapitalizeManualTranslations(manualTranslations);

                // This method replaces translation with the actual one
                var translationEntryKey = await GetTranslationKeyAsync(translationEntryAdditionInfo, cancellationToken).ConfigureAwait(false);
                if (translationEntryKey == null)
                {
                    return null;
                }

                var (translationResult, extendedTranslationResult) = await GetTranslationsAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);
                if (translationResult == null)
                {
                    return null;
                }

                var translationEntry = new TranslationEntry(translationEntryKey) { Id = translationEntryKey, ManualTranslations = manualTranslations };
                _translationEntryRepository.Upsert(translationEntry);

                var translationDetails = new TranslationDetails(translationResult, extendedTranslationResult, translationEntryKey);
                _translationDetailsRepository.Upsert(translationDetails);
                var learningInfo = _learningInfoRepository.GetOrInsert(translationEntryKey);

                var translationInfo = new TranslationInfo(translationEntry, translationDetails, learningInfo);

                if (needPostProcess)
                {
                    // no await here
                    // ReSharper disable once AssignmentIsFullyDiscarded
                    _ = PostProcessWordAsync(ownerWindow, translationInfo, cancellationToken).ConfigureAwait(false);
                }

                _logger.LogInformation("Processing finished for word {TranslationKey}", translationEntryKey);
                return translationInfo;
            }
            finally
            {
                executeInLoadingWindowContext?.Invoke(loadingWindow =>
                {
                    loadingWindow.Close();
                });
            }
        }

        async Task<ExtendedTranslationResult?> GetExtendedTranslationAsync(TranslationEntryKey translationEntryKey, CancellationToken cancellationToken)
        {
            // Used En as ui language to simplify conversion of common words to the enums
            var extendedTranslationResult = await _wordsTranslator.GetExtendedTranslationAsync(
                    translationEntryKey.Text,
                    translationEntryKey.SourceLanguage,
                    translationEntryKey.TargetLanguage,
                    LanguageConstants.EnLanguage,
                    ex => _messageHub.Publish(
                        string.Format(
                                CultureInfo.InvariantCulture,
                                Errors.CannotGetExtendedTranslation,
                                $"{translationEntryKey.Text} [{translationEntryKey.SourceLanguage}->{translationEntryKey.TargetLanguage}]")
                            .ToError(ex)),
                    cancellationToken)
                .ConfigureAwait(false);
            return extendedTranslationResult;
        }

        void DeleteFromPriority(TranslationEntry translationEntry, IReadOnlyCollection<ManualTranslation>? manualTranslations, TranslationDetails translationDetails)
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
            foreach (var deletedManualTranslation in deletedManualTranslations)
            {
                translationEntry.PriorityWords.Remove(deletedManualTranslation);
                deleted = true;
            }

            if (deleted)
            {
                _translationEntryRepository.Update(translationEntry);
            }
        }

        async Task<TranslationEntryKey?> GetTranslationKeyAsync(TranslationEntryAdditionInfo translationEntryAdditionInfo, CancellationToken cancellationToken)
        {
            var text = translationEntryAdditionInfo.Text;
            var sourceLanguage = translationEntryAdditionInfo.SourceLanguage;
            var targetLanguage = translationEntryAdditionInfo.TargetLanguage;
            if ((text == null) || string.IsNullOrWhiteSpace(text))
            {
                _messageHub.Publish(Errors.WordIsMissing.ToWarning());
                return null;
            }

            if ((sourceLanguage == null) || (sourceLanguage == LanguageConstants.AutoDetectLanguage))
            {
                sourceLanguage = await _languageManager.GetSourceAutoSubstituteAsync(text, cancellationToken).ConfigureAwait(false);
            }

            if ((targetLanguage == null) || (targetLanguage == LanguageConstants.AutoDetectLanguage))
            {
                targetLanguage = _languageManager.GetTargetAutoSubstitute(sourceLanguage);
            }
            else
            {
                if (!_languageManager.CheckTargetLanguageIsValid(sourceLanguage, targetLanguage))
                {
                    _messageHub.Publish(string.Format(CultureInfo.InvariantCulture, Errors.InvalidTargetLanguage, sourceLanguage, targetLanguage).ToWarning());
                    return null;
                }
            }

            return new TranslationEntryKey(text, sourceLanguage, targetLanguage);
        }

        async Task PostProcessWordAsync(IDisplayable? ownerWindow, TranslationInfo translationInfo, CancellationToken cancellationToken)
        {
            var playTtsTask = !_sharedSettingsRepository.MuteSounds
                ? _textToSpeechPlayerWrapper.PlayTtsAsync(
                    translationInfo.TranslationEntryKey.Text,
                    translationInfo.TranslationEntryKey.SourceLanguage,
                    cancellationToken)
                : Task.CompletedTask;
            var showCardTask = _cardManager.ShowCardAsync(translationInfo, ownerWindow);
            _messageHub.Publish(translationInfo.TranslationEntry);
            var updateCategoriesTask = _learningInfoCategoriesUpdater.UpdateLearningInfoClassificationCategoriesAsync(translationInfo, cancellationToken);
            await Task.WhenAll(playTtsTask, showCardTask, updateCategoriesTask).ConfigureAwait(false);
        }

        async Task<(TranslationDetails TranslationDetails, bool AlreadyExists)> ReloadTranslationDetailsIfNeededInternalAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            if (!manualTranslations?.Any() == true)
            {
                manualTranslations = null;
            }

            var translationDetails = _translationDetailsRepository.TryGetById(translationEntryKey);
            if (translationDetails?.ExtendedTranslationResult != null)
            {
                return (translationDetails, true);
            }

            if (translationDetails != null)
            {
                translationDetails.ExtendedTranslationResult = await GetExtendedTranslationAsync(translationEntryKey, cancellationToken).ConfigureAwait(false);
                _translationDetailsRepository.Update(translationDetails);
                return (translationDetails, true);
            }

            CapitalizeManualTranslations(manualTranslations);

            var (translationResult, extendedTranslationResult) = await GetTranslationsAsync(translationEntryKey, manualTranslations, cancellationToken).ConfigureAwait(false);
            if (translationResult == null)
            {
                throw new InvalidOperationException("Empty translation result for the existing translation entry");
            }

            // There are no translation details for this word in this thread, but some other thread might have updated them concurrently - in this case - just rewrite
            translationDetails = new TranslationDetails(translationResult, extendedTranslationResult, translationEntryKey);
            _translationDetailsRepository.Upsert(translationDetails);

            return (translationDetails, false);
        }

        async Task<TranslationResult?> TranslateAsync(TranslationEntryKey translationEntryKey, IReadOnlyCollection<ManualTranslation>? manualTranslations, CancellationToken cancellationToken)
        {
            // Used En as ui language to simplify conversion of common words to the enums
            var translationResult = await _wordsTranslator.GetTranslationAsync(
                    translationEntryKey.Text,
                    translationEntryKey.SourceLanguage,
                    translationEntryKey.TargetLanguage,
                    LanguageConstants.EnLanguage,
                    ex => _messageHub.Publish(
                        string.Format(
                                CultureInfo.InvariantCulture,
                                Errors.CannotTranslate,
                                $"{translationEntryKey.Text} [{translationEntryKey.SourceLanguage}->{translationEntryKey.TargetLanguage}]")
                            .ToError(ex)),
                    cancellationToken)
                .ConfigureAwait(false);

            if (translationResult == null)
            {
                return null;
            }

            if (translationResult.PartOfSpeechTranslations.Count > 0)
            {
                // replace the original text with the corrected one
                translationEntryKey.Text = translationResult.PartOfSpeechTranslations.First().Text;
                _logger.LogTrace("Received translation for {TranslationKey}", translationEntryKey);
            }
            else
            {
                if (manualTranslations == null)
                {
                    _messageHub.Publish(string.Format(CultureInfo.InvariantCulture, Errors.NoTranslations, translationEntryKey).ToWarning());
                    return null;
                }

                translationEntryKey.Text = translationEntryKey.Text.Capitalize();
            }

            if (manualTranslations != null)
            {
                translationResult.PartOfSpeechTranslations = ConcatTranslationsWithManual(translationEntryKey.Text, manualTranslations, translationResult.PartOfSpeechTranslations).ToArray();
            }

            return translationResult;
        }

        async Task<(TranslationResult? Basic, ExtendedTranslationResult? Extended)> GetTranslationsAsync(
            TranslationEntryKey translationEntryKey,
            IReadOnlyCollection<ManualTranslation>? manualTranslations,
            CancellationToken cancellationToken)
        {
            var getTranslationTask = TranslateAsync(translationEntryKey, manualTranslations, cancellationToken);
            var getExtendedTranslationTask = GetExtendedTranslationAsync(translationEntryKey, cancellationToken);

            await Task.WhenAll(getTranslationTask, getExtendedTranslationTask).ConfigureAwait(false);

            var translationResult = await getTranslationTask.ConfigureAwait(false);
            var extendedTranslationResult = await getExtendedTranslationTask.ConfigureAwait(false);
            return (translationResult, extendedTranslationResult);
        }
    }
}
