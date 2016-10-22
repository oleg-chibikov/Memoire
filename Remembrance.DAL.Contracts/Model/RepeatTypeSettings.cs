using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.DAL.Contracts.Model
{
    public static class RepeatTypeSettings
    {
        [NotNull]
        public static readonly LinkedList<RepeatType> RepeatTypes = new LinkedList<RepeatType>(new[]
        {
            RepeatType.T1,
            RepeatType.T2,
            RepeatType.T3,
            RepeatType.T4,
            RepeatType.T5,
            RepeatType.T6,
            RepeatType.T7,
            RepeatType.T8,
            RepeatType.T9
        });

        [NotNull]
        public static readonly Dictionary<RepeatType, TimeSpan> RepeatTimes = new Dictionary<RepeatType, TimeSpan>
        {
            { RepeatType.T1, TimeSpan.FromSeconds(5) },
            { RepeatType.T2, TimeSpan.FromMinutes(30) },
            { RepeatType.T3, TimeSpan.FromHours(1) },
            { RepeatType.T4, TimeSpan.FromDays(0.5) },
            { RepeatType.T5, TimeSpan.FromDays(1) },
            { RepeatType.T6, TimeSpan.FromDays(3.5) },
            { RepeatType.T7, TimeSpan.FromDays(7) },
            { RepeatType.T8, TimeSpan.FromDays(15) },
            { RepeatType.T9, TimeSpan.FromDays(30) }
        };
    }
}