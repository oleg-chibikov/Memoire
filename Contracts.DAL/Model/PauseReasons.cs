using System;

namespace Mémoire.Contracts.DAL.Model
{
    [Flags]
    public enum PauseReasons
    {
        None = 0,
        ActiveProcessBlacklisted = 1,
        OperationInProgress = 1 << 1,
        InactiveMode = 1 << 2,
        CardIsVisible = 1 << 3,
        CardIsLoading = 1 << 4
    }
}
