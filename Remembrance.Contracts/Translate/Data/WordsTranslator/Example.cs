using System.Collections.Generic;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class Example : TextEntry
    {
        public IReadOnlyCollection<TextEntry>? Translations { get; set; }
    }
}