using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Scar.Common.Notification;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable, IDisposable
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public TranslationEntryViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] TranslationEntryKey translationEntryKey)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            CanLearnWord = false;
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChangedAsync));

            ReloadTranslationsAsync().ConfigureAwait(false);
        }

        [DoNotNotify]
        public TranslationEntryKey Id { get; }

        public override string WordText => Id.Text;

        [CanBeNull]
        public ManualTranslation[] ManualTranslations { get; set; }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations { get; private set; }

        public int ShowCount { get; set; }

        [NotNull]
        public override string Language => Id.SourceLanguage;

        [NotNull]
        public string TargetLanguage => Id.TargetLanguage;

        public RepeatType RepeatType { get; set; }

        public DateTime LastCardShowTime { get; set; }

        public DateTime NextCardShowTime { get; set; }

        public bool IsFavorited { get; set; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messenger.UnSubscribe(subscriptionToken);
            }
        }

        public bool NotificationIsSupressed { get; set; }

        [NotNull]
        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }

        private async Task ProcessNonPriorityAsync([NotNull] WordKey wordKey)
        {
            var translations = Translations;
            _logger.TraceFormat("Removing non-priority word {1} from the list for {0}...", this, wordKey);
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (_wordsEqualityComparer.Equals(translation, wordKey))
                {
                    _logger.TraceFormat("Removing {0} from the list...", wordKey);
                    translations.RemoveAt(i--);
                }
            }

            if (!translations.Any())
            {
                _logger.Trace("No more translations left in the list. Restoring default...");
                await ReloadNonPriorityAsync().ConfigureAwait(false);
            }
        }

        private void ProcessPriority([NotNull] WordKey wordKey)
        {
            var translations = Translations;
            _logger.TraceFormat("Removing all non-priority translations for {0} except {1}...", this, wordKey);
            var found = false;
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];
                if (_wordsEqualityComparer.Equals(translation, wordKey))
                {
                    if (!translation.IsPriority)
                    {
                        _logger.TraceFormat("Found {0} in the list. Marking as priority...", wordKey);
                        translation.IsPriority = true;
                    }
                    else
                    {
                        _logger.TraceFormat("Found {0} in the list but it is already priority", wordKey);
                    }

                    found = true;
                }

                if (!translation.IsPriority)
                {
                    translations.RemoveAt(i--);
                }
            }

            if (!found)
            {
                _logger.TraceFormat("Not found {0} in the list. Adding...", wordKey);
                var copy = _viewModelAdapter.Adapt<PriorityWordViewModel>(wordKey);
                copy.SetTranslationEntryKey(wordKey);
                translations.Add(copy);
            }
        }

        private async void OnPriorityChangedAsync([NotNull] PriorityWordKey priorityWordKey)
        {
            _logger.TraceFormat("Changing priority for {0} in the list...", priorityWordKey);
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            var wordKey = priorityWordKey.WordKey;
            if (!wordKey.Equals(Id))
            {
                return;
            }

            if (priorityWordKey.IsPriority)
            {
                ProcessPriority(wordKey);
            }
            else
            {
                await ProcessNonPriorityAsync(wordKey).ConfigureAwait(false);
            }
        }

        private async Task ReloadNonPriorityAsync()
        {
            var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(Id, ManualTranslations, CancellationToken.None).ConfigureAwait(false);
            ReloadTranslations(translationDetails.TranslationResult.GetDefaultWords());
        }

        public async Task ReloadTranslationsAsync()
        {
            //If there are priority words - load only them
            var priorityWords = _wordPriorityRepository.GetPriorityWordsForTranslationEntry(Id);
            if (priorityWords.Any())
            {
                await ReloadPriorityAsync(priorityWords).ConfigureAwait(false);
            }
            //otherwise load default words
            else
            {
                await ReloadNonPriorityAsync().ConfigureAwait(false);
            }
        }

        public override string ToString()
        {
            return $"{Id}";
        }

        private IEnumerable<IWord> GetPriorityWordsInTranslationDetails(IWord[] priorityWords, [NotNull] TranslationDetails translationDetails)
        {
            foreach (var translationVariant in translationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (priorityWords.Any(priorityWord => _wordsEqualityComparer.Equals(translationVariant, priorityWord)))
                {
                    yield return translationVariant;
                }

                if (translationVariant.Synonyms == null)
                {
                    continue;
                }

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => priorityWords.Any(priorityWord => _wordsEqualityComparer.Equals(synonym, priorityWord))))
                {
                    yield return synonym;
                }
            }
        }

        private async Task ReloadPriorityAsync([NotNull] IWord[] priorityWords)
        {
            var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(Id, ManualTranslations, CancellationToken.None).ConfigureAwait(false);
            var priorityWordsDetails = GetPriorityWordsInTranslationDetails(priorityWords, translationDetails);
            ReloadTranslations(priorityWordsDetails);
        }

        private void ReloadTranslations([NotNull] IEnumerable<IWord> words)
        {
            var translations = _viewModelAdapter.Adapt<PriorityWordViewModel[]>(words);
            foreach (var translation in translations)
            {
                translation.SetTranslationEntryKey(Id);
            }

            Translations = new ObservableCollection<PriorityWordViewModel>(translations);
        }
    }
}