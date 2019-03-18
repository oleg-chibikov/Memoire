using JetBrains.Annotations;

namespace Remembrance.Contracts.CardManagement.Data
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        [CanBeNull]
        string GetPauseReasons();

        void Pause(PauseReason pauseReason, [CanBeNull] string? description = null);

        void ResetPauseTimes();

        void Resume(PauseReason pauseReason);
    }
}