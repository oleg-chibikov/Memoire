using System;

namespace Remembrance.Contracts.CardManagement
{
    public interface ICardShowTimeProvider
    {
        TimeSpan CardShowFrequency { get; }

        DateTime? LastCardShowTime { get; }

        DateTime NextCardShowTime { get; }

        TimeSpan TimeLeftToShowCard { get; }
    }
}
