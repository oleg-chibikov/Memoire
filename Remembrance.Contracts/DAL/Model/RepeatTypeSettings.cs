using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL.Model
{
    public static class RepeatTypeSettings
    {
        [NotNull]
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

        [NotNull]
        public static readonly Dictionary<RepeatType, TimeSpan> RepeatTimes = new Dictionary<RepeatType, TimeSpan>
        {
            {RepeatType.Elementary, TimeSpan.FromSeconds(5)},
            {RepeatType.Beginner, TimeSpan.FromMinutes(30)},
            {RepeatType.Novice, TimeSpan.FromHours(1)},
            {RepeatType.PreIntermediate, TimeSpan.FromDays(0.5)},
            {RepeatType.Intermediate, TimeSpan.FromDays(1)},
            {RepeatType.UpperIntermediate, TimeSpan.FromDays(3.5)},
            {RepeatType.Advanced, TimeSpan.FromDays(7)},
            {RepeatType.Proficiency, TimeSpan.FromDays(15)},
            {RepeatType.Expert, TimeSpan.FromDays(30)}
        };
    }
}