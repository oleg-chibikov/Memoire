using System.Collections.Generic;
using Remembrance.Contracts.ProcessMonitoring.Data;

namespace Remembrance.Contracts.ProcessMonitoring
{
    public interface IActiveProcessesProvider
    {
        IEnumerable<ProcessInfo> GetActiveProcesses();
    }
}