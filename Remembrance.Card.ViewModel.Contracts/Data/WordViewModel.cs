using System;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.WPF.Commands;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public class WordViewModel : TextEntryViewModel
    {
        [NotNull]
        private readonly ITextToSpeechPlayer textToSpeechPlayer;

        [NotNull]
        private readonly IWordsProcessor wordsProcessor;

        public WordViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
        {
            this.textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            this.wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            PlayTtsCommand = new CorrelationCommand(PlayTts);
            LearnWordCommand = new CorrelationCommand(LearnWord, () => CanLearnWord);
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
        public string WordInfo => string.Join(
            ", ",
            new[]
            {
                VerbType,
                NounAnimacy,
                NounGender
            }.Where(x => x != null));

        private void LearnWord()
        {
            wordsProcessor.ProcessNewWord(Text, Language);
        }

        private void PlayTts()
        {
            textToSpeechPlayer.PlayTtsAsync(Text, Language);
        }

        public override string ToString()
        {
            return $"{Text} [{Language}]";
        }
    }
}