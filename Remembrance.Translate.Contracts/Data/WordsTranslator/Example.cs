using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public sealed class Example : TextEntry
    {
        [CanBeNull]
        [UsedImplicitly]
        public TextEntry[] Translations { get; set; }
    }
}