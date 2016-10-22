using JetBrains.Annotations;

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public sealed class TranslationVariant : PriorityWord
    {
        [CanBeNull, UsedImplicitly]
        public PriorityWord[] Synonyms { get; set; }

        [CanBeNull, UsedImplicitly]
        public Word[] Meanings { get; set; }

        [CanBeNull, UsedImplicitly]
        public Example[] Examples { get; set; }
    }
}