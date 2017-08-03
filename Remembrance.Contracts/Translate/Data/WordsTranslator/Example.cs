using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class Example : TextEntry
    {
        [CanBeNull]
        [UsedImplicitly]
        public TextEntry[] Translations { get; set; }
    }
}