using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Core
{
    [UsedImplicitly]
    public sealed class WordsEqualityComparer : IEqualityComparer<IWord>
    {
        public bool Equals(IWord x, IWord y)
        {
            return x != null
                   && y != null
                   && x.Text.Equals(y.Text, StringComparison.InvariantCultureIgnoreCase)
                   && (x.PartOfSpeech == y.PartOfSpeech || x.PartOfSpeech == PartOfSpeech.Unknown || y.PartOfSpeech == PartOfSpeech.Unknown);
        }

        public int GetHashCode(IWord obj)
        {
            unchecked
            {
                var hashCode = obj.Text.ToLowerInvariant().GetHashCode();
                hashCode = (hashCode * 397) ^ obj.PartOfSpeech.GetHashCode();
                return hashCode;
            }
        }
    }
}