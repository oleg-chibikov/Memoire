using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.Translate;

namespace Remembrance.ViewModel.Translation
{
    public sealed class PartOfSpeechTranslationViewModel : WordViewModel
    {
        public PartOfSpeechTranslationViewModel([NotNull] ITextToSpeechPlayer textToSpeechPlayer, [NotNull] ITranslationEntryProcessor translationEntryProcessor)
            : base(textToSpeechPlayer, translationEntryProcessor)
        {
            CanLearnWord = false;
        }

        public string Transcription { get; set; }

        [NotNull]
        public TranslationVariantViewModel[] TranslationVariants { get; set; }
    }
}