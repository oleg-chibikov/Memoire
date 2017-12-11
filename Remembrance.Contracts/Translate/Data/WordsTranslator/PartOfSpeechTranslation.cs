using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class PartOfSpeechTranslation : Word
    {
        [CanBeNull]
        [UsedImplicitly]
        public string Transcription { get; set; }

        [NotNull]
        [UsedImplicitly]
        public TranslationVariant[] TranslationVariants { get; set; }

        [UsedImplicitly]
        public bool IsManual { get; set; }
    }
}