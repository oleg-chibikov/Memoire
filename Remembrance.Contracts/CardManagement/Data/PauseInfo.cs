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

        [CanBeNull]
        public DateTime? EndTime { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan GetPauseTime()
        {
            return (EndTime ?? DateTime.Now) - StartTime;
        }
    }
}