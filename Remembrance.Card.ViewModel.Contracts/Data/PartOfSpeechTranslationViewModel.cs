using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Translate.Contracts.Interfaces;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        // ReSharper disable once NotNullMemberIsNotInitialized
        public PartOfSpeechTranslationViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor) : base(textToSpeechPlayer, wordsProcessor)
        {
        }

        public string Transcription
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public TranslationVariantViewModel[] TranslationVariants
        {
            get;
            [UsedImplicitly]
            set;
        }
    }
}