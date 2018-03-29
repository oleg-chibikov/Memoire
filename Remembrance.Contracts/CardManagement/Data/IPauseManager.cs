using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement.Data
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        void Pause(PauseReason pauseReason, [CanBeNull] string description = null);

        void Resume(PauseReason pauseReason);

        void ResetPauseTimes();

        [CanBeNull]
        string GetPauseReasons();
    }
}