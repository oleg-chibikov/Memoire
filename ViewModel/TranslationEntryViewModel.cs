using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.DAL.Contracts.Model;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel
    {
        readonly ILogger _logger;
        readonly Func<Word, TranslationEntry, PriorityWordViewModel> _priorityWordViewModelFactory;
        readonly SynchronizationContext _synchronizationContext;
        readonly ITranslationEntryRepository _translationEntryRepository;

        public TranslationEntryViewModel(
            TranslationEntry translationEntry,
            ITextToSpeechPlayerWrapper textToSpeechPlayerWrapper,
            ITranslationEntryProcessor translationEntryProcessor,
            ILogger<TranslationEntryViewModel> logger,
            SynchronizationContext synchronizationContext,
            ITranslationEntryRepository translationEntryRepository,
            Func<Word, TranslationEntry, PriorityWordViewModel> priorityWordViewModelFactory,
            ILearningInfoRepository learningInfoRepository,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ILanguageManager languageManager,
            ICommandManager commandManager) : base(
            new Word { Text = translationEntry?.Id.Text ?? throw new ArgumentNullException(nameof(translationEntry)) },
            translationEntry.Id.SourceLanguage,
            textToSpeechPlayerWrapper,
            translationEntryProcessor,
            commandManager)
        {
            _ = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _ = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _ = learningInfoViewModelFactory ?? throw new ArgumentNullException(nameof(learningInfoViewModelFactory));
            _priorityWordViewModelFactory = priorityWordViewModelFactory ?? throw new ArgumentNullException(nameof(priorityWordViewModelFactory));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Id = translationEntry.Id;
            CanLearnWord = false;
            var learningInfo = learningInfoRepository.GetOrInsert(Id);
            LearningInfoViewModel = learningInfoViewModelFactory(learningInfo);
            var allLanguages = languageManager.GetAvailableLanguages();
            var sourceLanguageName = allLanguages[Language].ToLowerInvariant();
            var targetLanguageName = allLanguages[TargetLanguage].ToLowerInvariant();
            ReversoContextLink = string.Format(CultureInfo.InvariantCulture, Constants.ReversoContextUrlTemplate, sourceLanguageName, targetLanguageName, Word.Text.ToLowerInvariant());
            UpdateModifiedDate(learningInfo, translationEntry.ModifiedDate);
            HasManualTranslations = translationEntry.ManualTranslations?.Count > 0;

            // no await here
            ConstructionTask = ReloadTranslationsAsync(translationEntry);
        }

        public override string Language => Id.SourceLanguage;

        public bool HasManualTranslations { get; set; }

        [DoNotNotify]
        public TranslationEntryKey Id { get; }

        public LearningInfoViewModel LearningInfoViewModel { get; }

        public DateTimeOffset ModifiedDate { get; private set; }

        public string TargetLanguage => Id.TargetLanguage;

        public string ReversoContextLink { get; }

        public ObservableCollection<PriorityWordViewModel> Translations { get; } = new ();

        internal Task ConstructionTask { get; }

        public override string ToString()
        {
            return Id.ToString();
        }

        public void ProcessPriorityChange(PriorityWordKey priorityWordKey)
        {
            _ = priorityWordKey ?? throw new ArgumentNullException(nameof(priorityWordKey));
            if (priorityWordKey.IsPriority)
            {
                ProcessPriority(priorityWordKey.WordKey);
            }
            else
            {
                ProcessNonPriority(priorityWordKey.WordKey);
            }
        }

        public async Task ReloadTranslationsAsync(TranslationEntry translationEntry)
        {
            _ = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            var isPriority = translationEntry.PriorityWords?.Any() == true;
            IEnumerable<Word> words;
            if (isPriority)
            {
                words = translationEntry.PriorityWords!.Select(baseWord => new Word { Text = baseWord.Text, PartOfSpeech = baseWord.PartOfSpeech });
            }
            else
            {
                var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                    .ConfigureAwait(false);
                words = translationDetails.TranslationResult.GetDefaultWords();
            }

            var translations = words.Select(word => _priorityWordViewModelFactory(word, translationEntry));

            _synchronizationContext.Send(
                _ =>
                {
                    Translations.Clear();
                    foreach (var translation in translations)
                    {
                        Translations.Add(translation);
                    }
                },
                null);
        }

        public void Update(LearningInfo learningInfo, DateTimeOffset translationEntryModifiedDate)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            LearningInfoViewModel.UpdateLearningInfo(learningInfo);
            UpdateModifiedDate(learningInfo, translationEntryModifiedDate);
        }

        void ProcessNonPriority(WordKey wordKey)
        {
            var translations = Translations;
            _logger.LogTrace("Removing non-priority word {WordKey} from the list for {Translation}...", wordKey, this);
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (translation.Word.Equals(wordKey.Word))
                {
                    _logger.LogTrace("Removing {WordKey} from the list...", wordKey);

                    // ReSharper disable once AccessToModifiedClosure
                    _synchronizationContext.Send(_ => Translations.RemoveAt(i--), null);
                }
            }

            if (!(translations.Count > 0))
            {
                _logger.LogDebug("No more translations left in the list. Restoring default...");
                var translationEntry = _translationEntryRepository.GetById(Id);

                // no await here
                // ReSharper disable once AssignmentIsFullyDiscarded
                _ = ReloadTranslationsAsync(translationEntry);
            }
        }

        void ProcessPriority(WordKey wordKey)
        {
            _logger.LogTrace("Removing all non-priority translations for {Translation} except {WordKey}...", this, wordKey);
            var found = false;
            for (var i = 0; i < Translations.Count; i++)
            {
                var translation = Translations[i];
                if (translation.Word.Equals(wordKey.Word))
                {
                    if (!translation.IsPriority)
                    {
                        _logger.LogTrace("Found {WordKey0} in the list. Marking as priority...", wordKey);
                        translation.SetIsPriority(true);
                        _logger.LogDebug("{WordKey} has been marked as priority", wordKey);
                    }
                    else
                    {
                        _logger.LogDebug("Found {WordKey} in the list but it is already priority", wordKey);
                    }

                    found = true;
                }

                if (!translation.IsPriority)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    _synchronizationContext.Send(_ => Translations.RemoveAt(i--), null);
                }
            }

            if (!found)
            {
                _logger.LogTrace("{WordKey} is not found in the list. Adding...", wordKey);

                var translationEntry = _translationEntryRepository.GetById(wordKey.Key);
                var word = new Word { Text = wordKey.Word.Text, PartOfSpeech = wordKey.Word.PartOfSpeech };
                var priorityWordViewModel = _priorityWordViewModelFactory(word, translationEntry);
                _synchronizationContext.Send(_ => Translations.Add(priorityWordViewModel), null);
                _logger.LogDebug("{WordKey} has been added to the list", wordKey);
            }
        }

        void UpdateModifiedDate(ITrackedEntity learningInfo, DateTimeOffset translationEntryModifiedDate)
        {
            ModifiedDate = learningInfo.ModifiedDate > translationEntryModifiedDate ? learningInfo.ModifiedDate : translationEntryModifiedDate;
        }
    }
}
