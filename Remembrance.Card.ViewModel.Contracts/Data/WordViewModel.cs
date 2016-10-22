using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Contracts.Interfaces;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public class WordViewModel : TextEntryViewModel
    {
        [NotNull]
        private readonly ITextToSpeechPlayer textToSpeechPlayer;

        [NotNull]
        private readonly IWordsAdder wordsAdder;

        public WordViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsAdder wordsAdder)
        {
            if (textToSpeechPlayer == null)
                throw new ArgumentNullException(nameof(textToSpeechPlayer));
            if (wordsAdder == null)
                throw new ArgumentNullException(nameof(wordsAdder));
            this.textToSpeechPlayer = textToSpeechPlayer;
            this.wordsAdder = wordsAdder;
            PlayTtsCommand = new RelayCommand(PlayTts);
            LearnWordCommand = new RelayCommand(LearnWord, () => CanLearnWord);
        }

        public virtual string Language { get; set; }

        public ICommand PlayTtsCommand { get; }
        public ICommand LearnWordCommand { get; }
        public bool CanEdit => CanLearnWord || CanTogglePriority;
        public bool CanLearnWord { get; set; } = true;

        public virtual bool CanTogglePriority { get; } = false;

        public PartOfSpeech PartOfSpeech
        {
            get;
            [UsedImplicitly]
            set;
        }


        [CanBeNull]
        public string VerbType
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public string NounAnimacy
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public string NounGender
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public string WordInfo => string.Join(", ", new[] { VerbType, NounAnimacy, NounGender }.Where(x => x != null));

        public void PlayTts()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            textToSpeechPlayer.PlayTtsAsync(Text, Language);
        }

        public void LearnWord()
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            wordsAdder.AddWord(Text, Language);
        }

        public override string ToString()
        {
            return $"{Text} [{Language}]";
        }
    }
}