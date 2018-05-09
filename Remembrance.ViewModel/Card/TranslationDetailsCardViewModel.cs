using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.ViewModel.Translation;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsCardViewModel : IDisposable
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly IPredictor _predictor;

        [NotNull]
        private readonly IPrepositionsInfoRepository _prepositionsInfoRepository;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly TranslationEntry _translationEntry;

        public TranslationDetailsCardViewModel(
            [NotNull] Func<TranslationInfo, TranslationDetailsViewModel> translationDetailsViewModelFactory,
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messageHub,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository,
            [NotNull] IPredictor predictor,
            [NotNull] ILearningInfoRepository learningInfoRepository)
        {
            if (translationDetailsViewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(translationDetailsViewModelFactory));
            }

            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            var translationDetails = translationDetailsViewModelFactory(translationInfo);

            TranslationDetails = translationDetails ?? throw new ArgumentNullException(nameof(translationDetailsViewModelFactory));

            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));

            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _translationEntry = translationInfo.TranslationEntry;
            IsFavorited = translationInfo.LearningInfo.IsFavorited;
            Word = translationInfo.TranslationEntryKey.Text;

            // no await here
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = LoadPrepositionsIfNotExistsAsync(translationInfo.TranslationEntryKey.Text, translationInfo.TranslationDetails, CancellationToken.None);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<PriorityWordKey>(OnPriorityChanged));
            FavoriteCommand = new CorrelationCommand(Favorite);
        }

        [NotNull]
        public ICommand FavoriteCommand { get; }

        public bool IsFavorited { get; private set; }

        [CanBeNull]
        public PrepositionsCollection PrepositionsCollection { get; private set; }

        [NotNull]
        public TranslationDetailsViewModel TranslationDetails { get; }

        [NotNull]
        public string Word { get; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messageHub.UnSubscribe(subscriptionToken);
            }

            _subscriptionTokens.Clear();
        }

        private void Favorite()
        {
            _logger.TraceFormat("{0} {1}...", IsFavorited ? "Unfavoriting" : "Favoriting", TranslationDetails);
            var learningInfo = _learningInfoRepository.GetOrInsert(_translationEntry.Id);
            learningInfo.IsFavorited = IsFavorited = !IsFavorited;
            _learningInfoRepository.Update(learningInfo);
        }

        [ItemCanBeNull]
        [NotNull]
        private async Task<PrepositionsCollection> GetPrepositionsCollectionAsync([NotNull] string text, CancellationToken cancellationToken)
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

        [NotNull]
        private async Task LoadPrepositionsIfNotExistsAsync([NotNull] string text, [NotNull] TranslationDetails translationDetails, CancellationToken cancellationToken)
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

        private void OnPriorityChanged([NotNull] PriorityWordKey priorityWordKey)
        {
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            if (!priorityWordKey.WordKey.TranslationEntryKey.Equals(_translationEntry.Id))
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

        private async void OnUiLanguageChangedAsync([NotNull] CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            _logger.TraceFormat("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    () =>
                    {
                        CultureUtilities.ChangeCulture(cultureInfo);

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