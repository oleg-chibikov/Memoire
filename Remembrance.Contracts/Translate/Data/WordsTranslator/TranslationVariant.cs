using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationVariant : PriorityWord
    {
        [CanBeNull]
        [UsedImplicitly]
        public PriorityWord[] Synonyms { get; set; }

        [CanBeNull]
        [UsedImplicitly]
        public Word[] Meanings { get; set; }

        [CanBeNull]
        [UsedImplicitly]
        public Example[] Examples { get; set; }
    }
}