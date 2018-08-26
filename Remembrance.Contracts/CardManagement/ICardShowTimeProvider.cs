using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement
{
    public interface ICardShowTimeProvider
    {
        TimeSpan CardShowFrequency { get; }

        [CanBeNull]
        DateTime? LastCardShowTime { get; }

        DateTime NextCardShowTime { get; }

        TimeSpan TimeLeftToShowCard { get; }
    }
}