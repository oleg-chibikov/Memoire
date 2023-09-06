using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.ProcessMonitoring;
using Microsoft.Extensions.Logging;

namespace Mémoire.Core.ProcessMonitoring
{
    public sealed class ActiveProcessesProvider : IActiveProcessesProvider
    {
        public ActiveProcessesProvider(ILogger<ActiveProcessesProvider> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public IEnumerable<ProcessInfo> GetActiveProcesses()
        {
            using var searcher = new ManagementObjectSearcher("SELECT ProcessId, ExecutablePath FROM Win32_Process");
            using var results = searcher.Get();
            var processes = from process in Process.GetProcesses()
                join managementObject in results.Cast<ManagementObject>() on process.Id equals (int)(uint)managementObject["ProcessId"]
                let filePath = (string)managementObject["ExecutablePath"]
                where filePath != null
                select new ProcessInfo(process.ProcessName, filePath);

            return processes.GroupBy(p => (p.Name, FileName: p.FilePath)).Select(g => g.First()).OrderBy(processInfo => processInfo.Name).ToArray();
        }
    }
}
