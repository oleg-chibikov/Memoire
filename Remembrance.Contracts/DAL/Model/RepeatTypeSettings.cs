using System;
using System.Collections.Generic;

namespace Remembrance.Contracts.DAL.Model
{
    public static class RepeatTypeSettings
    {
        public static readonly Dictionary<RepeatType, TimeSpan> RepeatTimes = new Dictionary<RepeatType, TimeSpan>
        {
            { RepeatType.Elementary, TimeSpan.FromMinutes(5) },
            { RepeatType.Beginner, TimeSpan.FromHours(6) },
            { RepeatType.Novice, TimeSpan.FromHours(12) },
            { RepeatType.PreIntermediate, TimeSpan.FromDays(2) },
            { RepeatType.Intermediate, TimeSpan.FromDays(5) },
            { RepeatType.UpperIntermediate, TimeSpan.FromDays(10) },
            { RepeatType.Advanced, TimeSpan.FromDays(30) },
            { RepeatType.Proficiency, TimeSpan.FromDays(50) },
            { RepeatType.Expert, TimeSpan.FromDays(100) }
        };

        public static readonly LinkedList<RepeatType> RepeatTypes = new LinkedList<RepeatType>(
            new[]
            {
                RepeatType.Elementary,
                RepeatType.Beginner,
                RepeatType.Novice,
                RepeatType.PreIntermediate,
                RepeatType.Intermediate,
                RepeatType.UpperIntermediate,
                RepeatType.Advanced,
                RepeatType.Proficiency,
                RepeatType.Expert
            });
    }
}
