namespace Mémoire.Contracts.DAL.Model
{
    public sealed class PauseReasonAndState : PauseState
    {
        public PauseReasonAndState(PauseReason pauseReason, bool isPaused) : base(isPaused)
        {
            PauseReason = pauseReason;
        }

        public PauseReason PauseReason { get; }
    }
}
