using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationVariant : Word
    {
        [CanBeNull]
        public Word[] Synonyms { get; set; }

        [CanBeNull]
        public Word[] Meanings { get; set; }

        [CanBeNull]
        public Example[] Examples { get; set; }
    }
}