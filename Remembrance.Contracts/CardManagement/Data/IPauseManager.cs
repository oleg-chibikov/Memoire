namespace Remembrance.Contracts.CardManagement.Data
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoCollection GetPauseInfo(PauseReasons pauseReasons);

        string? GetPauseReasons();

        void PauseActivity(PauseReasons pauseReasons, string? description = null);

        void ResetPauseTimes();

        void ResumeActivity(PauseReasons pauseReasons);
    }
}
