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
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
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
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IPredictor _predictor;

        [NotNull]
        private readonly IPrepositionsInfoRepository _prepositionsInfoRepository;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly TranslationEntry _translationEntry;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        public TranslationDetailsCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository,
            [NotNull] IPredictor predictor)
        {
            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            if (viewModelAdapter == null)
            {
                throw new ArgumentNullException(nameof(viewModelAdapter));
            }

            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _prepositionsInfoRepository = prepositionsInfoRepository ?? throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            _predictor = predictor ?? throw new ArgumentNullException(nameof(predictor));

            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TranslationDetails = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            _translationEntry = translationInfo.TranslationEntry;
            IsFavorited = _translationEntry.IsFavorited;
            Word = translationInfo.TranslationEntryKey.Text;
            LoadPrepositionsIfNotExistsAsync(translationInfo.TranslationEntryKey.Text, translationInfo.TranslationDetails, CancellationToken.None).ConfigureAwait(false);

            _subscriptionTokens.Add(messenger.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChanged));
            FavoriteCommand = new CorrelationCommand(Favorite);
        }

        [CanBeNull]
        public PrepositionsCollection PrepositionsCollection { get; private set; }

        [NotNull]
        public ICommand FavoriteCommand { get; }

        [NotNull]
        public TranslationDetailsViewModel TranslationDetails { get; }

        [NotNull]
        public string Word { get; }

        public bool IsFavorited { get; private set; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messenger.UnSubscribe(subscriptionToken);
            }
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
                    foreach (var translationVariant in TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
                    {
                        if (translationVariant.Equals(priorityWordKey.WordKey.Word))
                        {
                            _logger.TraceFormat("Setting priority: {0} in translations details for translation variant...", priorityWordKey);
                            translationVariant.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }

                        if (translationVariant.Synonyms == null)
                        {
                            continue;
                        }

                        foreach (var synonym in translationVariant.Synonyms.Where(synonym => synonym.Equals(priorityWordKey.WordKey.Word)))
                        {
                            _logger.TraceFormat("Setting priority: {0} in translations details for synonym...", priorityWordKey);
                            synonym.SetIsPriority(priorityWordKey.IsPriority);
                            return;
                        }
                    }
                },
                CancellationToken.None);
        }

        [ItemCanBeNull]
        private async Task<PrepositionsCollection> GetPrepositionsCollectionAsync([NotNull] string text, CancellationToken cancellationToken)
        {
            var predictionResult = await _predictor.PredictAsync(text, 5, cancellationToken).ConfigureAwait(false);
            if (predictionResult == null)
            {
                return null;
            }

            var prepositionsCollection = new PrepositionsCollection
            {
                Texts = predictionResult.Position > 0
                    ? predictionResult.PredictionVariants
                    : null
            };
            return prepositionsCollection;
        }

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
                        partOfSpeechTranslation.ReRender();
                        foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                        {
                            translationVariant.ReRender();
                            if (translationVariant.Synonyms != null)
                            {
                                foreach (var synonym in translationVariant.Synonyms)
                                {
                                    synonym.ReRender();
                                }
                            }

                            if (translationVariant.Meanings != null)
                            {
                                foreach (var meaning in translationVariant.Meanings)
                                {
                                    meaning.ReRender();
                                }
                            }
                        }
                    }
                },
                CancellationToken.None);
        }

        private void Favorite()
        {
            _logger.TraceFormat(
                "{0} {1}...",
                IsFavorited
                    ? "Unfavoriting"
                    : "Favoriting",
                TranslationDetails);
            _translationEntry.IsFavorited = IsFavorited = !IsFavorited;
            _translationEntryRepository.Update(_translationEntry);
        }
    }
}