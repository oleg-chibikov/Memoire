using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
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
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly TranslationEntry _translationEntry;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public TranslationDetailsCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] ILog logger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IMessageHub messenger,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] IPrepositionsInfoRepository prepositionsInfoRepository,
            [NotNull] IWordsProcessor wordsProcessor)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));
            if (prepositionsInfoRepository == null)
                throw new ArgumentNullException(nameof(prepositionsInfoRepository));
            if (wordsProcessor == null)
                throw new ArgumentNullException(nameof(wordsProcessor));

            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));

            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            wordsProcessor.ReloadAdditionalInfoAsync(translationInfo.Key.Text, translationInfo.TranslationDetails, CancellationToken.None);
            TranslationDetails = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            _translationEntry = translationInfo.TranslationEntry;
            IsFavorited = _translationEntry.IsFavorited;
            Word = translationInfo.Key.Text;
            PrepositionsCollection = prepositionsInfoRepository.GetPrepositionsInfo(translationInfo.TranslationEntry.Id)
                ?.Prepositions;

            _subscriptionTokens.Add(messenger.Subscribe<CultureInfo>(OnUiLanguageChanged));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordViewModel>(OnPriorityChanged));
            _subscriptionTokens.Add(messenger.Subscribe<PrepositionsInfo>(OnPrepositionsInfoReceived));
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
            //TODO not called when it is not the DataContext of the window!
            foreach (var subscriptionToken in _subscriptionTokens)
                _messenger.UnSubscribe(subscriptionToken);
        }

        private void OnPrepositionsInfoReceived([NotNull] PrepositionsInfo prepositionsInfo)
        {
            if (Equals(prepositionsInfo.TranslationEntryId, _translationEntry.Id))
            {
                _logger.InfoFormat("Received prepositions for {0}", this);
                PrepositionsCollection = prepositionsInfo.Prepositions;
            }
        }

        [CanBeNull]
        private PriorityWordViewModel GetWordInTranslationDetails(IWord word)
        {
            foreach (var translationVariant in TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (_wordsEqualityComparer.Equals(translationVariant, word))
                    return translationVariant;

                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => _wordsEqualityComparer.Equals(synonym, word)))
                    return synonym;
            }

            return null;
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            if (!priorityWordViewModel.TranslationEntryId.Equals(TranslationDetails.TranslationEntryId))
                return;

            _logger.TraceFormat("Priority changed for {0}. Updating the word in translation details...", priorityWordViewModel);
            var translation = GetWordInTranslationDetails(priorityWordViewModel);
            if (translation != null)
            {
                _logger.TraceFormat("Priority for {0} is updated", translation);
                translation.IsPriority = priorityWordViewModel.IsPriority;
            }
            else
            {
                _logger.Trace("There is no matching translation in the card");
            }
        }

        private void OnUiLanguageChanged([NotNull] CultureInfo cultureInfo)
        {
            _logger.TraceFormat("Changing UI language to {0}...", cultureInfo);
            if (cultureInfo == null)
                throw new ArgumentNullException(nameof(cultureInfo));

            CultureUtilities.ChangeCulture(cultureInfo);

            foreach (var partOfSpeechTranslation in TranslationDetails.TranslationResult.PartOfSpeechTranslations)
            {
                partOfSpeechTranslation.ReRender();
                foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                {
                    translationVariant.ReRender();
                    if (translationVariant.Synonyms != null)
                        foreach (var synonym in translationVariant.Synonyms)
                            synonym.ReRender();
                    if (translationVariant.Meanings != null)
                        foreach (var meaning in translationVariant.Meanings)
                            meaning.ReRender();
                }
            }
        }

        private void Favorite()
        {
            var text = IsFavorited
                ? "Unfavoriting"
                : "Favoriting";
            _logger.TraceFormat("{0} {1}...", text, TranslationDetails);
            _translationEntry.IsFavorited = IsFavorited = !IsFavorited;
            _translationEntryRepository.Save(_translationEntry);
        }
    }
}