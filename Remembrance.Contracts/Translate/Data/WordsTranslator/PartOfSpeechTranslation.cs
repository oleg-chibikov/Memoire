using System.Collections.Generic;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class PartOfSpeechTranslation : Word
    {
        public bool IsManual { get; set; }

        public string? Transcription { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<TranslationVariant> TranslationVariants { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
