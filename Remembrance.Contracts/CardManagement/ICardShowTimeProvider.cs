using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement
{
    public interface ICardShowTimeProvider
    {
        TimeSpan CardShowFrequency { get; }

        bool IsPaused { get; }

        [CanBeNull]
        DateTime? LastCardShowTime { get; }

        DateTime LastPausedTime { get; }

        DateTime NextCardShowTime { get; }

        TimeSpan PausedTime { get; }

        TimeSpan TimeLeftToShowCard { get; }
    }
}