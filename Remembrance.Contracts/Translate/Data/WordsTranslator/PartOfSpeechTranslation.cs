using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class PartOfSpeechTranslation : Word
    {
        [CanBeNull]
        public string Transcription { get; set; }

        [NotNull]
        public ICollection<TranslationVariant> TranslationVariants { get; set; }

        public bool IsManual { get; set; }
    }
}