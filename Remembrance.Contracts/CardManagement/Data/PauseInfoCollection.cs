using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement.Data
{
    public sealed class PauseInfoCollection : List<PauseInfo>
    {
        public PauseInfoCollection()
        {
        }

        public PauseInfoCollection([NotNull] IEnumerable<PauseInfo> collection)
            : base(collection)
        {
        }

        public TimeSpan GetPauseTime()
        {
            return this.Any() ? this.Select(pauseInfo => pauseInfo.GetPauseTime()).Aggregate((a, b) => b + a) : TimeSpan.Zero;
        }

        public bool IsPaused()
        {
            var last = this.LastOrDefault();
            return last != null && last.EndTime == null;
        }

        public bool Pause()
        {
            var last = this.LastOrDefault();
            if (last != null && last.EndTime == null)
            {
                return false;
            }

            Add(new PauseInfo(DateTime.Now));
            return true;
        }

        public bool Resume()
        {
            var last = this.LastOrDefault();
            if (last == null || last.EndTime != null)
            {
                return false;
            }

            last.EndTime = DateTime.Now;
            return true;
        }
    }
}