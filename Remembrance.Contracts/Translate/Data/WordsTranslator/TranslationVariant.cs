using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationVariant : Word
    {
        [CanBeNull]
        [UsedImplicitly]
        public Word[] Synonyms { get; set; }

        [CanBeNull]
        [UsedImplicitly]
        public Word[] Meanings { get; set; }

        [CanBeNull]
        [UsedImplicitly]
        public Example[] Examples { get; set; }
    }
}