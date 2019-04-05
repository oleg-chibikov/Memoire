using System;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class WordImageSearchIndex : TrackedEntity<WordKey>
    {
        public WordImageSearchIndex()
        {
        }

        public WordImageSearchIndex(WordKey wordKey, int searchIndex, bool isAlternate)
        {
            Id = wordKey ?? throw new ArgumentNullException(nameof(wordKey));
            SearchIndex = searchIndex;
            IsAlternate = isAlternate;
        }

        public bool IsAlternate { get; set; }

        public int SearchIndex { get; set; }

        public override string ToString()
        {
            return $"Image search index for {Id})";
        }
    }
}