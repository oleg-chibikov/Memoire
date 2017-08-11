using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Scar.Common;
using Scar.Common.Notification;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationEntryViewModel : WordViewModel, INotificationSupressable
    {
        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        private string _text;

        [UsedImplicitly]
        public TranslationEntryViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            CanLearnWord = false;
        }

        [UsedImplicitly]
        [DoNotNotify]
        public object Id { get; set; }

        [UsedImplicitly]
        public override string Text
        {
            get => _text ?? string.Empty;
            set
            {
                var newValue = value.Capitalize();
                if (newValue == _text)
                    return;

                // For the new item this event should not be fired
                if (_text != null && !NotificationIsSupressed)
                {
                    var handler = Volatile.Read(ref TextChanged);
                    handler?.Invoke(this, new TextChangedEventArgs(newValue, _text));
                }
                else
                {
                    _text = newValue;
                }
            }
        }

        [NotNull]
        public ObservableCollection<PriorityWordViewModel> Translations { get; private set; }

        [UsedImplicitly]
        public int ShowCount { get; set; }

        [NotNull]
        [UsedImplicitly]
        public override string Language { get; set; }

        [NotNull]
        [UsedImplicitly]
        public string TargetLanguage { get; set; }

        [UsedImplicitly]
        public RepeatType RepeatType { get; set; }

        [UsedImplicitly]
        public DateTime LastCardShowTime { get; set; }

        [UsedImplicitly]
        public DateTime NextCardShowTime { get; set; }

        public bool NotificationIsSupressed { get; set; }

        [NotNull]
        public NotificationSupresser SupressNotification()
        {
            return new NotificationSupresser(this);
        }

        private IEnumerable<IWord> GetPriorityWordsInTranslationDetails(IWord[] priorityWords)
        {
            var translationDetails = WordsProcessor.ReloadTranslationDetailsIfNeeded(Id, Text, Language, TargetLanguage);
            foreach (var translationVariant in translationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (priorityWords.Any(priorityWord => _wordsEqualityComparer.Equals(translationVariant, priorityWord)))
                    yield return translationVariant;

                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => priorityWords.Any(priorityWord => _wordsEqualityComparer.Equals(synonym, priorityWord))))
                    yield return synonym;
            }
        }

        public void ReloadNonPriority()
        {
            var translationDetails = WordsProcessor.ReloadTranslationDetailsIfNeeded(Id, Text, Language, TargetLanguage);
            var defaultWords = translationDetails.TranslationResult.GetDefaultWords();
            var translations = _viewModelAdapter.Adapt<PriorityWordViewModel[]>(defaultWords);
            foreach (var translation in translations)
                translation.SetProperties(Id, TargetLanguage, false);

            Translations = new ObservableCollection<PriorityWordViewModel>(translations);
        }

        private void ReloadPriority([NotNull] IWord[] priorityWords)
        {
            var translations = _viewModelAdapter.Adapt<PriorityWordViewModel[]>(GetPriorityWordsInTranslationDetails(priorityWords));
            foreach (var translation in translations)
                translation.SetProperties(Id, TargetLanguage, true);

            Translations = new ObservableCollection<PriorityWordViewModel>(translations);
        }

        public void ReloadTranslations()
        {
            //If there are priority words - load only them
            var priorityWords = _wordPriorityRepository.GetPriorityWordsForTranslationEntry(Id);
            if (priorityWords.Any())
                ReloadPriority(priorityWords);
            //otherwise load default words
            else
                ReloadNonPriority();
        }

        public event TextChangedEventHandler TextChanged;

        public override string ToString()
        {
            return $"{Text} [{Language}->{TargetLanguage}]";
        }
    }
}