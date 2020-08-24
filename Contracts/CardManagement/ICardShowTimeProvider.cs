using System;

namespace Mémoire.Contracts.CardManagement
{
    public interface ICardShowTimeProvider
    {
        TimeSpan CardShowFrequency { get; }

        DateTimeOffset? LastCardShowTime { get; }

        DateTimeOffset NextCardShowTime { get; }

        TimeSpan TimeLeftToShowCard { get; }
    }
}
