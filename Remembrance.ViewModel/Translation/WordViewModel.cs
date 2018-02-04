using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : BaseWord
    {
        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        public WordViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] ITranslationEntryProcessor translationEntryProcessor)
        {
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            PlayTtsCommand = new CorrelationCommand(PlayTtsAsync);
            LearnWordCommand = new CorrelationCommand(LearnWordAsync, () => CanLearnWord);
            TogglePriorityCommand = new CorrelationCommand(TogglePriority);
        }

        [NotNull]
        public ICommand TogglePriorityCommand { get; }

        public bool IsPriority { get; set; }

        [DoNotNotify]
        public virtual string Language { get; set; }

        [DoNotNotify]
        public bool CanLearnWord { get; set; } = true;

        [DoNotNotify]
        public virtual bool CanEdit => CanLearnWord;

        /// <summary>
        /// A hack to raise NotifyPropertyChanged for other properties
        /// </summary>
        [AlsoNotifyFor(nameof(PartOfSpeech))]
        private bool ReRenderSwitch { get; set; }

        [DoNotNotify]
        public override PartOfSpeech PartOfSpeech { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public string VerbType { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public string NounAnimacy { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public string NounGender { get; set; }

        [CanBeNull]
        public string WordInfo =>
            VerbType == null && NounAnimacy == null && NounGender == null
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

        private async void LearnWordAsync()
        {
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Text, Language), CancellationToken.None).ConfigureAwait(false);
        }

        private async void PlayTtsAsync()
        {
            await _textToSpeechPlayer.PlayTtsAsync(Text, Language, CancellationToken.None).ConfigureAwait(false);
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
            return $"{base.ToString()} [{Language}]";
        }
    }
}