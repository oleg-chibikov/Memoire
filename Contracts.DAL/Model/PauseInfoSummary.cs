using System;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseInfoSummary
    {
        TimeSpan _prevPauseTime = TimeSpan.Zero;
        PauseInfo? _currentPauseInfo;

        public TimeSpan PauseTime => _prevPauseTime + _currentPauseInfo?.PauseTime ?? TimeSpan.Zero;

        public bool IsPaused() => _currentPauseInfo != null;

        public bool Pause()
        {
            if (IsPaused())
            {
                return false;
            }

            _currentPauseInfo = new PauseInfo(DateTimeOffset.Now);
            return true;
        }

        public bool Resume()
        {
            if (!IsPaused())
            {
                return false;
            }

            _prevPauseTime = _prevPauseTime.Add(DateTimeOffset.Now - _currentPauseInfo!.StartTime);
            _currentPauseInfo = null;
            return true;
        }

        public void Clear()
        {
            _currentPauseInfo = null;
            _prevPauseTime = TimeSpan.Zero;
        }
    }
}
