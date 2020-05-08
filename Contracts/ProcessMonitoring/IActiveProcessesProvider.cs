using System.Collections.Generic;
using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts.ProcessMonitoring
{
    public interface IActiveProcessesProvider
    {
        IEnumerable<ProcessInfo> GetActiveProcesses();
    }
}
