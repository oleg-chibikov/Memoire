using System;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseInfo
    {
        public PauseInfo(DateTime startTime, DateTime? endTime = null)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public DateTime? EndTime { get; set; }

        public DateTime StartTime { get; set; }

        public TimeSpan GetPauseTime()
        {
            return (EndTime ?? DateTime.Now) - StartTime;
        }
    }
}
