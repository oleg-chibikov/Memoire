using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Autofac;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
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
        protected readonly ILifetimeScope LifetimeScope;

        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        public WordViewModel(
            [NotNull] Word word,
            [NotNull] string language,
            [NotNull] ILifetimeScope lifetimeScope,
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

            LifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));

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

        [NotNull]
        public ICommand TogglePriorityCommand { get; }

        public bool IsPriority { get; protected set; }

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
        public string VerbType { get; }

        [CanBeNull]
        [DoNotNotify]
        public string NounAnimacy { get; }

        [CanBeNull]
        [DoNotNotify]
        public string NounGender { get; }

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

        private async Task LearnWordAsync()
        {
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(Text, Language), CancellationToken.None).ConfigureAwait(false);
        }

        private async Task PlayTtsAsync()
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