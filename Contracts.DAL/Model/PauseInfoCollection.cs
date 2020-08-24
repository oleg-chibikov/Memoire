using System;
using System.Collections.Generic;
using System.Linq;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseInfoCollection : List<PauseInfo>
    {
        public PauseInfoCollection()
        {
        }

        public PauseInfoCollection(IEnumerable<PauseInfo> collection) : base(collection)
        {
        }

        public TimeSpan GetPauseTime()
        {
            return Count > 0 ? this.Select(pauseInfo => pauseInfo.GetPauseTime()).Aggregate((a, b) => b + a) : TimeSpan.Zero;
        }

        public bool IsPaused()
        {
            var last = this.LastOrDefault();
            return (last != null) && (last.EndTime == null);
        }

        public bool Pause()
        {
            var last = this.LastOrDefault();
            if ((last != null) && (last.EndTime == null))
            {
                return false;
            }

            Add(new PauseInfo(DateTimeOffset.Now));
            return true;
        }

        public bool Resume()
        {
            var last = this.LastOrDefault();
            if ((last == null) || (last.EndTime != null))
            {
                return false;
            }

            last.EndTime = DateTimeOffset.Now;
            return true;
        }
    }
}
