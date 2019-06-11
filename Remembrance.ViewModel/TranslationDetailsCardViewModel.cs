using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsCardViewModel : BaseViewModel
    {
        private readonly ICultureManager _cultureManager;

        private readonly ILog _logger;

        private readonly IMessageHub _messageHub;

        private readonly IPredictor _predictor;

        private readonly IPrepositionsInfoRepository _prepositionsInfoRepository;

        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        private readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsCardViewModel(
            TranslationInfo translationInfo,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            Func<TranslationInfo, TranslationDetailsViewModel> translationDetailsViewModelFactory,
            ILog logger,
            IMessageHub messageHub,
            IPrepositionsInfoRepository prepositionsInfoRepository,
            IPredictor predictor,
            ILanguageManager languageManager,
            ICultureManager cultureManager,
            ICommandManager commandManager)
            : base(commandManager)
        {
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
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
            ReversoContextLink = string.Format(Constants.ReversoContextUrlTemplate, sourceLanguageName, targetLanguageName, Word.ToLowerInvariant());
            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = LoadPrepositionsIfNotExistsAsync(translationInfo.TranslationEntryKey.Text, translationInfo.TranslationDetails, CancellationToken.None);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<PriorityWordKey>(OnPriorityChanged));
            _subscriptionTokens.Add(messageHub.Subscribe<LearningInfo>(OnLearningInfoReceivedAsync));
        }

        public string LanguagePair { get; }

        public string ReversoContextLink { get; }

        public LearningInfoViewModel LearningInfoViewModel { get; }

        public PrepositionsCollection? PrepositionsCollection { get; private set; }

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

        private async Task<PrepositionsCollection?> GetPrepositionsCollectionAsync(string text, CancellationToken cancellationToken)
        {
            var predictionResult = await _predictor.PredictAsync(text, 5, cancellationToken).ConfigureAwait(false);
            if (predictionResult == null)
            {
                return null;
            }

            var prepositionsCollection = new PrepositionsCollection
            {
                Texts = predictionResult.Position > 0 ? predictionResult.PredictionVariants : null
            };
            return prepositionsCollection;
        }

        private async Task LoadPrepositionsIfNotExistsAsync(string text, TranslationDetails translationDetails, CancellationToken cancellationToken)
        {
            var prepositionsInfo = _prepositionsInfoRepository.TryGetById(translationDetails.Id);
            if (prepositionsInfo == null)
            {
                _logger.TraceFormat("Reloading preposition for {0}...", translationDetails.Id);
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

        private async void OnLearningInfoReceivedAsync(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            if (!learningInfo.Id.Equals(_translationEntryKey))
            {
                return;
            }

            _logger.DebugFormat("Received {0} from external source", learningInfo);

            await Task.Run(() => LearningInfoViewModel.UpdateLearningInfo(learningInfo), CancellationToken.None).ConfigureAwait(false);
        }

        private void OnPriorityChanged(PriorityWordKey priorityWordKey)
        {
            _ = priorityWordKey ?? throw new ArgumentNullException(nameof(priorityWordKey));
            if (!priorityWordKey.WordKey.TranslationEntryKey.Equals(_translationEntryKey))
            {
                return;
            }

            Task.Run(
                () =>
                {
                    foreach (var translationVariant in TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(
                        partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
                    {
                        if (translationVariant.Word.Equals(priorityWordKey.WordKey.Word))
                        {
                            _logger.TraceFormat("Setting priority: {0} in translations details for translation variant...", priorityWordKey);
                            translationVariant.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }

                        if (translationVariant.Synonyms == null)
                        {
                            continue;
                        }

                        foreach (var synonym in translationVariant.Synonyms.Where(synonym => synonym.Word.Equals(priorityWordKey.WordKey.Word)))
                        {
                            _logger.TraceFormat("Setting priority: {0} in translations details for synonym...", priorityWordKey);
                            synonym.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }
                    }
                },
                CancellationToken.None);
        }

        private async void OnUiLanguageChangedAsync(CultureInfo cultureInfo)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            _logger.TraceFormat("Changing UI language to {0}...", cultureInfo);

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
                .ConfigureAwait(false);
        }
    }
}