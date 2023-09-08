namespace Mémoire.Contracts.DAL.Model;

public class PauseState
{
    public PauseState(bool isPaused)
    {
        IsPaused = isPaused;
    }

    public bool IsPaused { get; }
}
