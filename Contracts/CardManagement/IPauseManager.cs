using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts.CardManagement
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoSummary GetPauseInfo(PauseReasons pauseReasons);

        string? GetPauseReasons();

        void PauseActivity(PauseReasons pauseReasons, string? description = null);

        void ResetPauseTimes();

        void ResumeActivity(PauseReasons pauseReasons);
    }
}
