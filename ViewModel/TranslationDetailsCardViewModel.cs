using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.Classification;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Localization;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts;
using Scar.Services.Contracts.Data.Classification;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsCardViewModel : BaseViewModel
    {
        readonly ICultureManager _cultureManager;
        readonly ILanguageDetector _languageDetector;
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly IPredictor _predictor;
        readonly IPrepositionsInfoRepository _prepositionsInfoRepository;
        readonly IList<Guid> _subscriptionTokens = new List<Guid>();
        readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsCardViewModel(
            TranslationInfo translationInfo,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            Func<TranslationInfo, TranslationDetailsViewModel> translationDetailsViewModelFactory,
            ILogger<TranslationDetailsCardViewModel> logger,
            IMessageHub messageHub,
            IPrepositionsInfoRepository prepositionsInfoRepository,
            IPredictor predictor,
            ILanguageManager languageManager,
            ICultureManager cultureManager,
            ICommandManager commandManager,
            ILearningInfoCategoriesUpdater learningInfoCategoriesUpdater,
            ILanguageDetector languageDetector) : base(commandManager)
        {
            _ = learningInfoCategoriesUpdater ?? throw new ArgumentNullException(nameof(learningInfoCategoriesUpdater));
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _ = translationDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsViewModelFactory));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _ = learningInfoViewModelFactory ?? throw new ArgumentNullException(nameof(learningInfoViewModelFactory));
            _ = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            var translationDetails = translationDetailsViewModelFactory(translationInfo);
            _translationEntryKey = translationInfo.TranslationEntryKey;
            TranslationDetails = translationDetails ?? throw new ArgumentNullException(nameof(translationDetailsViewModelFactory));
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LearningInfoViewModel = learningInfoViewModelFactory(translationInfo.LearningInfo);

            Word = translationInfo.TranslationEntryKey.Text;
            LanguagePair = $"{translationInfo.TranslationEntryKey.SourceLanguage} -> {translationInfo.TranslationEntryKey.TargetLanguage}";
            var allLanguages = languageManager.GetAvailableLanguages();
            var sourceLanguageName = allLanguages[translationInfo.TranslationEntryKey.SourceLanguage].ToLowerInvariant();
            var targetLanguageName = allLanguages[translationInfo.TranslationEntryKey.TargetLanguage].ToLowerInvariant();
            ReversoContextLink = string.Format(CultureInfo.InvariantCulture, Constants.ReversoContextUrlTemplate, sourceLanguageName, targetLanguageName, Word.ToLowerInvariant());

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = LoadPrepositionsIfNotExistsAsync(translationInfo.TranslationEntryKey.Text, translationInfo.TranslationDetails, CancellationToken.None);

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = LoadClassificationCategoriesIfNotExistsAsync(translationInfo, learningInfoCategoriesUpdater);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(HandleUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<PriorityWordKey>(HandlePriorityChanged));
            _subscriptionTokens.Add(messageHub.Subscribe<LearningInfo>(HandleLearningInfoReceivedAsync));
        }

        public string LanguagePair { get; }

        public string ReversoContextLink { get; }

        public LearningInfoViewModel LearningInfoViewModel { get; }

        public Prepositions? PrepositionsCollection { get; private set; }

        public IReadOnlyCollection<ClassificationCategory>? ClassificationCategories { get; set; }

        public TranslationDetailsViewModel TranslationDetails { get; }

        public string Word { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                foreach (var subscriptionToken in _subscriptionTokens)
                {
                    _messageHub.Unsubscribe(subscriptionToken);
                }

                _subscriptionTokens.Clear();
            }
        }

        async Task<Prepositions?> GetPrepositionsCollectionAsync(string text, CancellationToken cancellationToken)
        {
            var language = await _languageDetector.DetectLanguageAsync(text, ex => _messageHub.Publish(Errors.CannotDetectLanguage.ToError(ex)), cancellationToken).ConfigureAwait(false);
            var predictionResult = await _predictor.PredictAsync(text, 5, language.Language, ex => _messageHub.Publish(Errors.CannotPredict.ToError(ex)), cancellationToken).ConfigureAwait(false);
            if (predictionResult == null)
            {
                return null;
            }

            var prepositionsCollection = new Prepositions { Texts = predictionResult.Position > 0 ? predictionResult.PredictionVariants : null };
            return prepositionsCollection;
        }

        async Task LoadPrepositionsIfNotExistsAsync(string text, TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            var prepositionsInfo = _prepositionsInfoRepository.TryGetById(translationDetails.Id);
            if (prepositionsInfo == null)
            {
                _logger.LogTrace("Reloading preposition for {0}...", translationDetails.Id);
                var prepositions = await GetPrepositionsCollectionAsync(text, cancellationToken).ConfigureAwait(false);
                if (prepositions == null)
                {
                    return;
                }

                prepositionsInfo = new PrepositionsInfo(translationDetails.Id, prepositions);
                _prepositionsInfoRepository.Insert(prepositionsInfo);
            }

            PrepositionsCollection = prepositionsInfo.Prepositions;
        }

        async Task LoadClassificationCategoriesIfNotExistsAsync(TranslationInfo translationInfo, ILearningInfoCategoriesUpdater learningInfoCategoriesUpdater)
        {
            await learningInfoCategoriesUpdater.UpdateLearningInfoClassificationCategoriesAsync(translationInfo, CancellationToken.None).ConfigureAwait(false);
            ClassificationCategories = translationInfo.LearningInfo.ClassificationCategories?.Items.ToArray() ?? Array.Empty<ClassificationCategory>();
        }

        async void HandleLearningInfoReceivedAsync(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            if (!learningInfo.Id.Equals(_translationEntryKey))
            {
                return;
            }

            _logger.LogDebug("Received {0} from external source", learningInfo);

            await Task.Run(() => LearningInfoViewModel.UpdateLearningInfo(learningInfo), CancellationToken.None).ConfigureAwait(true);
        }

        void HandlePriorityChanged(PriorityWordKey priorityWordKey)
        {
            _ = priorityWordKey ?? throw new ArgumentNullException(nameof(priorityWordKey));
            if (!priorityWordKey.WordKey.TranslationEntryKey.Equals(_translationEntryKey))
            {
                return;
            }

            Task.Run(
                () =>
                {
                    foreach (var translationVariant in TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
                    {
                        if (translationVariant.Word.Equals(priorityWordKey.WordKey.Word))
                        {
                            _logger.LogTrace("Setting priority: {0} in translations details for translation variant...", priorityWordKey);
                            translationVariant.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }

                        if (translationVariant.Synonyms == null)
                        {
                            continue;
                        }

                        foreach (var synonym in translationVariant.Synonyms.Where(synonym => synonym.Word.Equals(priorityWordKey.WordKey.Word)))
                        {
                            _logger.LogTrace("Setting priority: {0} in translations details for synonym...", priorityWordKey);
                            synonym.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }
                    }
                },
                CancellationToken.None);
        }

        async void HandleUiLanguageChangedAsync(CultureInfo cultureInfo)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            _logger.LogTrace("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    () =>
                    {
                        _cultureManager.ChangeCulture(cultureInfo);

                        foreach (var partOfSpeechTranslation in TranslationDetails.TranslationResult.PartOfSpeechTranslations)
                        {
                            partOfSpeechTranslation.ReRenderWord();
                            foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                            {
                                translationVariant.ReRenderWord();
                                if (translationVariant.Synonyms != null)
                                {
                                    foreach (var synonym in translationVariant.Synonyms)
                                    {
                                        synonym.ReRenderWord();
                                    }
                                }

                                if (translationVariant.Meanings != null)
                                {
                                    foreach (var meaning in translationVariant.Meanings)
                                    {
                                        meaning.ReRenderWord();
                                    }
                                }
                            }
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
    }
}
