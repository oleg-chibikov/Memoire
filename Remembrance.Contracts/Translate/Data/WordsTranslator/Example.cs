using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class Example : TextEntry
    {
        [CanBeNull]
        public TextEntry[] Translations { get; set; }
    }
}