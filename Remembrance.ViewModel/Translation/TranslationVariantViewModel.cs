using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
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
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ViewModelAdapter viewModelAdapter,
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