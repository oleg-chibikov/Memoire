using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class Example : TextEntry
    {
        [CanBeNull]
        [UsedImplicitly]
        public TextEntry[] Translations { get; set; }
    }
}