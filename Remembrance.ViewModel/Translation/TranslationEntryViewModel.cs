using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly Func<Word, TranslationEntry, PriorityWordViewModel> _priorityWordViewModelFactory;

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        public TranslationEntryViewModel(
            [NotNull] TranslationEntry translationEntry,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] Func<Word, TranslationEntry, PriorityWordViewModel> priorityWordViewModelFactory,
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(
                new Word
                {
                    Text = translationEntry.Id.Text
                },
                translationEntry.Id.SourceLanguage,
                textToSpeechPlayer,
                translationEntryProcessor)
        {
            if (translationEntry == null)
            {
                throw new ArgumentNullException(nameof(translationEntry));
            }

            _priorityWordViewModelFactory = priorityWordViewModelFactory ?? throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Id = translationEntry.Id;
            CanLearnWord = false;
            var learningInfo = learningInfoRepository.GetOrInsert(Id);
            UpdateLearningInfo(learningInfo);

            // no await here
            ConstructionTask = ReloadTranslationsAsync(translationEntry);
        }

        [DoNotNotify]
        public TranslationEntryKey Id { get; }

        public bool IsFavorited { get; set; }

        [NotNull]
        public override string Language => Id.SourceLanguage;

        public DateTime LastCardShowTime { get; set; }

        public DateTime NextCardShowTime { get; set; }

        public RepeatType RepeatType { get; set; }

        public int ShowCount { get; set; }

        [NotNull]
        public string TargetLanguage => Id.TargetLanguage;

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations { get; } = new ObservableCollection<PriorityWordViewModel>();

        [NotNull]
        internal Task ConstructionTask { get; }

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(NextCardShowTime))]
        private bool ReRenderNextCardShowTimeSwitch { get; set; }

        public void ProcessPriorityChange([NotNull] PriorityWordKey priorityWordKey)
        {
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            if (priorityWordKey.IsPriority)
            {
                ProcessPriority(priorityWordKey.WordKey);
            }
            else
            {
                ProcessNonPriority(priorityWordKey.WordKey);
            }
        }

        [NotNull]
        public async Task ReloadTranslationsAsync([NotNull] TranslationEntry translationEntry)
        {
            if (translationEntry == null)
            {
                throw new ArgumentNullException(nameof(translationEntry));
            }

            var isPriority = translationEntry.PriorityWords?.Any() == true;
            IEnumerable<Word> words;
            if (isPriority)
            {
                words = translationEntry.PriorityWords.Select(
                    baseWord => new Word
                    {
                        Text = baseWord.Text,
                        PartOfSpeech = baseWord.PartOfSpeech
                    });
            }
            else
            {
                var translationDetails = await TranslationEntryProcessor
                    .ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                    .ConfigureAwait(false);
                words = translationDetails.TranslationResult.GetDefaultWords();
            }

            var translations = words.Select(word => _priorityWordViewModelFactory(word, translationEntry));

            _synchronizationContext.Send(
                x =>
                {
                    Translations.Clear();
                    foreach (var translation in translations)
                    {
                        Translations.Add(translation);
                    }
                },
                null);
        }

        public void ReRenderNextCardShowTime()
        {
            ReRenderNextCardShowTimeSwitch = !ReRenderNextCardShowTimeSwitch;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public void UpdateLearningInfo([NotNull] LearningInfo learningInfo)
        {
            IsFavorited = learningInfo.IsFavorited;
            LastCardShowTime = learningInfo.LastCardShowTime;
            NextCardShowTime = learningInfo.NextCardShowTime;
            RepeatType = learningInfo.RepeatType;
            ShowCount = learningInfo.ShowCount;
        }

        private void ProcessNonPriority([NotNull] WordKey wordKey)
        {
            var translations = Translations;
            _logger.TraceFormat("Removing non-priority word {1} from the list for {0}...", this, wordKey);
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (translation.Word.Equals(wordKey.Word))
                {
                    _logger.TraceFormat("Removing {0} from the list...", wordKey);

                    // ReSharper disable once AccessToModifiedClosure
                    _synchronizationContext.Send(x => Translations.RemoveAt(i--), null);
                }
            }

            if (!translations.Any())
            {
                _logger.Debug("No more translations left in the list. Restoring default...");
                var translationEntry = _translationEntryRepository.GetById(Id);

                // no await here
                // ReSharper disable once AssignmentIsFullyDiscarded
                _ = ReloadTranslationsAsync(translationEntry);
            }
        }

        private void ProcessPriority([NotNull] WordKey wordKey)
        {
            _logger.TraceFormat("Removing all non-priority translations for {0} except {1}...", this, wordKey);
            var found = false;
            for (var i = 0; i < Translations.Count; i++)
            {
                var translation = Translations[i];
                if (translation.Word.Equals(wordKey.Word))
                {
                    if (!translation.IsPriority)
                    {
                        _logger.TraceFormat("Found {0} in the list. Marking as priority...", wordKey);
                        translation.SetIsPriority(true);
                        _logger.DebugFormat("{0} has been marked as priority", wordKey);
                    }
                    else
                    {
                        _logger.DebugFormat("Found {0} in the list but it is already priority", wordKey);
                    }

                    found = true;
                }

                if (!translation.IsPriority)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    _synchronizationContext.Send(x => Translations.RemoveAt(i--), null);
                }
            }

            if (!found)
            {
                _logger.TraceFormat("Not found {0} in the list. Adding...", wordKey);

                var translationEntry = _translationEntryRepository.GetById(wordKey.TranslationEntryKey);
                var word = new Word
                {
                    Text = wordKey.Word.Text,
                    PartOfSpeech = wordKey.Word.PartOfSpeech
                };
                var priorityWordViewModel = _priorityWordViewModelFactory(word, translationEntry);
                _synchronizationContext.Send(x => Translations.Add(priorityWordViewModel), null);
                _logger.DebugFormat("{0} has been added to the list", wordKey);
            }
        }
    }
}