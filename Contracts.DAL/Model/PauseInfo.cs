using System;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseInfo
    {
        public PauseInfo(DateTimeOffset startTime, DateTimeOffset? endTime = null)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public DateTimeOffset? EndTime { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public TimeSpan PauseTime => (EndTime ?? DateTimeOffset.Now) - StartTime;
    }
}
