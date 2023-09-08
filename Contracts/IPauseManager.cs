using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoSummary GetPauseInfo(PauseReason pauseReason);

        string? GetPauseReasons();

        void PauseActivity(PauseReason pauseReason, string? description = null);

        void ResetPauseTimes();

        void ResumeActivity(PauseReason pauseReason);
    }
}
