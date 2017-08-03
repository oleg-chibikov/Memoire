using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.Translate.Contracts.Interfaces;
using Remembrance.TypeAdapter.Contracts;

namespace Remembrance.ViewModel
{
    public sealed class TranslationVariantViewModel : PriorityWordViewModel
    {
        public TranslationVariantViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessenger messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger)
            : base(textToSpeechPlayer, translationEntryRepository, translationDetailsRepository, viewModelAdapter, messenger, wordsProcessor, logger)
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