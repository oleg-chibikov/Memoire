using System.Collections.Generic;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationVariant : Word
    {
        public IReadOnlyCollection<Example>? Examples { get; set; }

        public IReadOnlyCollection<Word>? Meanings { get; set; }

        public IReadOnlyCollection<Word>? Synonyms { get; set; }
    }
}
