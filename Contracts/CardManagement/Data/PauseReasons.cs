using System;

namespace Remembrance.Contracts.CardManagement.Data
{
    [Flags]
    public enum PauseReasons
    {
        None = 0,
        ActiveProcessBlacklisted = 1,
        OperationInProgress = 1 << 1,
        InactiveMode = 1 << 2,
        CardIsVisible = 1 << 3
    }
}
