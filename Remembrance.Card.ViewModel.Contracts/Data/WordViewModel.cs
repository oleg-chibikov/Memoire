using System;
using System.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Card.Management.Contracts;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.WPF.Commands;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : TextEntryViewModel
    {
        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public WordViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
        {
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            PlayTtsCommand = new CorrelationCommand(PlayTts);
            LearnWordCommand = new CorrelationCommand(LearnWord, () => CanLearnWord);
        }

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

        [DoNotNotify]
        public PartOfSpeech PartOfSpeech
        {
            get;
            [UsedImplicitly]
            set;
        }

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

        private void LearnWord()
        {
            _wordsProcessor.ProcessNewWord(Text, Language);
        }

        private void PlayTts()
        {
            _textToSpeechPlayer.PlayTtsAsync(Text, Language);
        }

        public void ReRender()
        {
            ReRenderSwitch = !ReRenderSwitch;
        }

        public override string ToString()
        {
            return $"{Text} [{Language}]";
        }

        #region Commands

        public ICommand PlayTtsCommand { get; }

        public ICommand LearnWordCommand { get; }

        #endregion
    }
}