using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;

namespace Remembrance.ViewModel.Translation
{
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IMessageHub messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IWordPriorityRepository wordPriorityRepository)
            : base(textToSpeechPlayer, messenger, wordsProcessor, logger, wordPriorityRepository)
        {
        }

        [CanBeNull]
        public PriorityWordViewModel[] Synonyms
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public WordViewModel[] Meanings
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public ExampleViewModel[] Examples
        {
            get;
            [UsedImplicitly]
            set;
        }
    }
}