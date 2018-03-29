using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.ProcessMonitoring.Data;

namespace Remembrance.Contracts.ProcessMonitoring
{
    public interface IActiveProcessesProvider
    {
        [NotNull]
        ICollection<ProcessInfo> GetActiveProcesses();
    }
}
