using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Contracts
{
    [UsedImplicitly]
    public sealed class WordsEqualityComparer : IEqualityComparer<IWord>
    {
        public bool Equals([CanBeNull] IWord x, [CanBeNull] IWord y)
        {
            return x != null && y != null && x.Text == y.Text && (x.PartOfSpeech == y.PartOfSpeech || x.PartOfSpeech == PartOfSpeech.Unknown || y.PartOfSpeech == PartOfSpeech.Unknown);
        }

        public int GetHashCode([NotNull] IWord obj)
        {
            unchecked
            {
                var hashCode = obj.Text.GetHashCode();
                hashCode = (hashCode * 397) ^ obj.PartOfSpeech.GetHashCode();
                return hashCode;
            }
        }
    }
}