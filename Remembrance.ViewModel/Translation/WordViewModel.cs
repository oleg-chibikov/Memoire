using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Commands;

// ReSharper disable VirtualMemberCallInConstructor
namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public class WordViewModel : BaseWord
    {
        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        public WordViewModel(
            [NotNull] Word word,
            [NotNull] string language,
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor)
        {
            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            if (textToSpeechPlayer == null)
            {
                throw new ArgumentNullException(nameof(textToSpeechPlayer));
            }

            if (translationEntryProcessor == null)
            {
                throw new ArgumentNullException(nameof(translationEntryProcessor));
            }

            Text = word.Text;
            Language = language ?? throw new ArgumentNullException(nameof(language));
            PartOfSpeech = word.PartOfSpeech;
            NounAnimacy = word.NounAnimacy;
            NounGender = word.NounGender;
            VerbType = word.VerbType;

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
        public virtual string Language { get; set; }

        [NotNull]
        public ICommand LearnWordCommand { get; }

        [CanBeNull]
        [DoNotNotify]
        public string NounAnimacy { get; }

        [CanBeNull]
        [DoNotNotify]
        public string NounGender { get; }

        [DoNotNotify]
        public override PartOfSpeech PartOfSpeech { get; set; }

        [NotNull]
        public ICommand PlayTtsCommand { get; }

        [NotNull]
        public ICommand TogglePriorityCommand { get; }

        [CanBeNull]
        [DoNotNotify]
        public string VerbType { get; }

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

        // A hack to raise NotifyPropertyChanged for other properties
        [AlsoNotifyFor(nameof(PartOfSpeech))]
        private bool ReRenderSwitch { get; set; }

        public void ReRender()
        {
            ReRenderSwitch = !ReRenderSwitch;
        }

        public override string ToString()
        {
            return $"{base.ToString()} [{Language}]";
        }

        protected virtual void TogglePriority()
        {
        }

        [NotNull]
        private async Task LearnWordAsync()
        {
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Text, Language), CancellationToken.None).ConfigureAwait(false);
        }

        [NotNull]
        private async Task PlayTtsAsync()
        {
            await _textToSpeechPlayer.PlayTtsAsync(Text, Language, CancellationToken.None).ConfigureAwait(false);
        }
    }
}