namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseReasonAndState
    {
        public PauseReasonAndState(PauseReasons pauseReason, bool isPaused)
        {
            PauseReason = pauseReason;
            IsPaused = isPaused;
        }

        public PauseReasons PauseReason { get; }

        public bool IsPaused { get; }
    }
}
