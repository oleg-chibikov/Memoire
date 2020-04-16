using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using PropertyChanged;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

// ReSharper disable VirtualMemberCallInConstructor
namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : BaseViewModel
    {
        readonly ITextToSpeechPlayer _textToSpeechPlayer;

        public WordViewModel(
            Word word,
            string language,
            ITextToSpeechPlayer textToSpeechPlayer,
            ITranslationEntryProcessor translationEntryProcessor,
            ICommandManager commandManager) : base(commandManager)
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Word = word ?? throw new ArgumentNullException(nameof(word));

            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            PlayTtsCommand = AddCommand(PlayTtsAsync);
            LearnWordCommand = AddCommand(LearnWordAsync, () => CanLearnWord);
            TogglePriorityCommand = AddCommand(TogglePriority);
        }

        [DoNotNotify]
        public virtual bool CanEdit => CanLearnWord;

        [DoNotNotify]
        public bool CanLearnWord { get; set; } = true;

        public bool IsPriority { get; protected set; }

        [DoNotNotify]
        public virtual string Language { get; }

        public ICommand LearnWordCommand { get; }

        public ICommand PlayTtsCommand { get; }

        public int? PriorityGroup { get; protected set; }

        public ICommand TogglePriorityCommand { get; }

        [DoNotNotify]
        public Word Word { get; }

        public string? WordInfo =>
            (Word.VerbType == null) && (Word.NounAnimacy == null) && (Word.NounGender == null)
                ? null
                : string.Join(
                    ", ",
                    new[]
                    {
                        Word.VerbType,
                        Word.NounAnimacy,
                        Word.NounGender
                    }.Where(x => x != null));

        protected ITranslationEntryProcessor TranslationEntryProcessor { get; }

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(Word))]
        bool ReRenderWordSwitch { get; set; }

        public override string ToString()
        {
            return $"{Word} [{Language}]";
        }

        public void ReRenderWord()
        {
            ReRenderWordSwitch = !ReRenderWordSwitch;
        }

        protected virtual void TogglePriority()
        {
        }

        async Task LearnWordAsync()
        {
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Word.Text, Language), CancellationToken.None).ConfigureAwait(false);
        }

        async Task PlayTtsAsync()
        {
            await _textToSpeechPlayer.PlayTtsAsync(Word.Text, Language, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
