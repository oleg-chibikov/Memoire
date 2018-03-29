using System;
using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class PauseInfo
    {
        public PauseInfo(DateTime startTime, [CanBeNull] DateTime? endTime = null)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public DateTime StartTime { get; set; }

        [CanBeNull]
        public DateTime? EndTime { get; set; }

        public TimeSpan GetPauseTime() => (EndTime ?? DateTime.Now) - StartTime;
    }
}
