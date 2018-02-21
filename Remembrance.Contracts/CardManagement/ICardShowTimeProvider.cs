using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement
{
    public interface ICardShowTimeProvider
    {
        TimeSpan TimeLeftToShowCard { get; }

        TimeSpan CardShowFrequency { get; }

        [CanBeNull]
        DateTime? LastCardShowTime { get; }

        DateTime NextCardShowTime { get; }

        DateTime LastPausedTime { get; }

        TimeSpan PausedTime { get; }

        bool IsPaused { get; }
    }
}