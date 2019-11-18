using System;

namespace Remembrance.Contracts.CardManagement.Data
{
    [Flags]
    public enum PauseReason
    {
        None = 0,
        ActiveProcessBlacklisted = 1,
        OperationInProgress = 1 << 1,
        InactiveMode = 1 << 2,
        CardIsVisible = 1 << 3
    }

    public static class PauseReasonExtensions
    {
        public static bool IsPaused(this PauseReason pauseReason)
        {
            return pauseReason != PauseReason.None;
        }
    }
}