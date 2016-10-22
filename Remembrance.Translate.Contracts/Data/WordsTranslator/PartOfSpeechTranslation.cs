using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public sealed class PartOfSpeechTranslation : Word
    {
        [CanBeNull, UsedImplicitly]
        public string Transcription { get; set; }

        [NotNull, UsedImplicitly]
        public TranslationVariant[] TranslationVariants { get; set; }
    }
}