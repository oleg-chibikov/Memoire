using System;

namespace Remembrance.Contracts.CardManagement.Data
{
    [Flags]
    public enum PauseReason
    {
        /// <summary>
        /// No pause modificators
        /// </summary>
        None = 0,

        /// <summary>
        /// Paused because of the active process.
        /// </summary>
        ActiveProcessBlacklisted = 1 << 0,

        /// <summary>
        /// Paused because of the operation in progress.
        /// </summary>
        OperationInProgress = 1 << 1,

        /// <summary>
        /// Paused because of the inactive mode.
        /// </summary>
        InactiveMode = 1 << 2,

        /// <summary>
        /// Paused because the card is being shown now.
        /// </summary>
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