namespace Remembrance.Contracts.CardManagement.Data
{
    public interface IPauseManager
    {
        bool IsPaused { get; }

        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        string? GetPauseReasons();

        void Pause(PauseReason pauseReason, string? description = null);

        void ResetPauseTimes();

        void Resume(PauseReason pauseReason);
    }
}