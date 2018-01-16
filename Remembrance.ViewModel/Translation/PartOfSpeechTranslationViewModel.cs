using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.Translate;

namespace Remembrance.ViewModel.Translation
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] IWordsProcessor wordsProcessor)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            CanLearnWord = false;
        }

        public string Transcription { get; set; }

        [NotNull]
        public TranslationVariantViewModel[] TranslationVariants { get; set; }
    }
}