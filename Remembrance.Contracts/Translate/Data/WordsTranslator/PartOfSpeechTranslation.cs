using System.Collections.Generic;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class PartOfSpeechTranslation : Word
    {
        public bool IsManual { get; set; }

        public string? Transcription { get; set; }

        public IReadOnlyCollection<TranslationVariant> TranslationVariants { get; set; }
    }
}