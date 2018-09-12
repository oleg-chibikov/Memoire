using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Commands;

// ReSharper disable VirtualMemberCallInConstructor
namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel
    {
        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        public WordViewModel(
            [NotNull] Word word,
            [NotNull] string language,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor)
        {
            _ = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _ = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Word = word ?? throw new ArgumentNullException(nameof(word));

            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            PlayTtsCommand = new AsyncCorrelationCommand(PlayTtsAsync);
            LearnWordCommand = new AsyncCorrelationCommand(LearnWordAsync, () => CanLearnWord);
            TogglePriorityCommand = new CorrelationCommand(TogglePriority);
        }

        [DoNotNotify]
        public virtual bool CanEdit => CanLearnWord;

        [DoNotNotify]
        public bool CanLearnWord { get; set; } = true;

        public bool IsPriority { get; protected set; }

        [DoNotNotify]
        public virtual string Language { get; }

        [NotNull]
        public ICommand LearnWordCommand { get; }

        [NotNull]
        public ICommand PlayTtsCommand { get; }

        public int? PriorityGroup { get; protected set; }

        [NotNull]
        public ICommand TogglePriorityCommand { get; }

        [DoNotNotify]
        public Word Word { get; }

        [CanBeNull]
        public string WordInfo =>
            Word.VerbType == null && Word.NounAnimacy == null && Word.NounGender == null
                ? null
                : string.Join(
                    ", ",
                    new[]
                    {
                        Word.VerbType,
                        Word.NounAnimacy,
                        Word.NounGender
                    }.Where(x => x != null));

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(Word))]
        private bool ReRenderWordSwitch { get; set; }

        public void ReRenderWord()
        {
            ReRenderWordSwitch = !ReRenderWordSwitch;
        }

        public override string ToString()
        {
            return $"{Word} [{Language}]";
        }

        protected virtual void TogglePriority()
        {
        }

        [NotNull]
        private async Task LearnWordAsync()
        {
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Word.Text, Language), CancellationToken.None).ConfigureAwait(false);
        }

        [NotNull]
        private async Task PlayTtsAsync()
        {
            await _textToSpeechPlayer.PlayTtsAsync(Word.Text, Language, CancellationToken.None).ConfigureAwait(false);
        }
    }
}