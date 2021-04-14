using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Mémoire.Contracts;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts.Data.Translation;

// ReSharper disable VirtualMemberCallInConstructor
namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : BaseViewModel
    {
        readonly ITextToSpeechPlayerWrapper _textToSpeechPlayerWrapper;

        public WordViewModel(
            Word word,
            string language,
            ITextToSpeechPlayerWrapper textToSpeechPlayerWrapper,
            ITranslationEntryProcessor translationEntryProcessor,
            ICommandManager commandManager) : base(commandManager)
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Word = word ?? throw new ArgumentNullException(nameof(word));

            _textToSpeechPlayerWrapper = textToSpeechPlayerWrapper ?? throw new ArgumentNullException(nameof(textToSpeechPlayerWrapper));
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
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Word.Text, Language)).ConfigureAwait(false);
        }

        async Task PlayTtsAsync()
        {
            await _textToSpeechPlayerWrapper.PlayTtsAsync(
                    Word.Text,
                    Language,
                    CancellationToken.None)
                .ConfigureAwait(false);
        }
    }
}
