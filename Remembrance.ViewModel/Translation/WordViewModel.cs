using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : TextEntryViewModel, IWord
    {
        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        protected readonly IWordsProcessor WordsProcessor;

        public WordViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
        {
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            WordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            PlayTtsCommand = new CorrelationCommand(PlayTts);
            LearnWordCommand = new CorrelationCommand(LearnWord, () => CanLearnWord);
            TogglePriorityCommand = new CorrelationCommand(TogglePriority);
        }

        [NotNull]
        public ICommand TogglePriorityCommand { get; }

        public bool IsPriority { get; set; }

        [DoNotNotify]
        public virtual string Language { get; set; }

        [DoNotNotify]
        public bool CanLearnWord { get; set; } = true;

        public virtual bool CanEdit => CanLearnWord;

        /// <summary>
        /// A hack to raise NotifyPropertyChanged for other properties
        /// </summary>
        [AlsoNotifyFor(nameof(PartOfSpeech))]
        private bool ReRenderSwitch { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public string VerbType
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        [DoNotNotify]
        public string NounAnimacy
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        [DoNotNotify]
        public string NounGender
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public string WordInfo => VerbType == null && NounAnimacy == null && NounGender == null
            ? null
            : string.Join(
                ", ",
                new[]
                {
                    VerbType,
                    NounAnimacy,
                    NounGender
                }.Where(x => x != null));

        public ICommand PlayTtsCommand { get; }

        public ICommand LearnWordCommand { get; }

        [DoNotNotify]
        public PartOfSpeech PartOfSpeech
        {
            get;
            [UsedImplicitly]
            set;
        }

        private void LearnWord()
        {
            WordsProcessor.AddOrChangeWord(Text, Language);
        }

        private void PlayTts()
        {
            _textToSpeechPlayer.PlayTtsAsync(Text, Language, CancellationToken.None);
        }

        public void ReRender()
        {
            ReRenderSwitch = !ReRenderSwitch;
        }

        protected virtual void TogglePriority()
        {
        }

        public override string ToString()
        {
            return $"{Text} [{Language}]";
        }
    }
}