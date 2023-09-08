using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.ProcessMonitoring;
using Microsoft.Extensions.Logging;

namespace Mémoire.Core.ProcessMonitoring
{
    public sealed class ActiveProcessMonitor : IActiveProcessMonitor, IDisposable
    {
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly IPauseManager _pauseManager;
        readonly Timer _timer;

        public ActiveProcessMonitor(IPauseManager pauseManager, ILocalSettingsRepository localSettingsRepository, ILogger<ActiveProcessMonitor> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            CheckActiveProcess();

            // Adding additional mechanizm to check active process since Automation can be unresponsive sometimes
            _timer = new Timer(1000);
            _timer.Start();
            _timer.Elapsed += Timer_Tick;

            // Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public void Dispose()
        {
            // Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChangedHandler);
            _timer.Elapsed -= Timer_Tick;
            _timer.Stop();
            _timer.Dispose();
        }

        static Process? GetActiveProcess()
        {
            var hwnd = GetForegroundWindow();
            return GetProcessByHandle(hwnd);
        }

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        static extern IntPtr GetForegroundWindow();

        static Process? GetProcessByHandle(IntPtr hwnd)
        {
            try
            {
                var result = GetWindowThreadProcessId(hwnd, out var processId);
                return Process.GetProcessById((int)processId);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception)
            {
                return null;
            }
        }

        [DllImport("user32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

        // ReSharper disable once StyleCop.SA1305
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        void CheckActiveProcess()
        {
            var activeProcess = GetActiveProcess();
            if (activeProcess != null)
            {
                PauseOrResumeProcess(activeProcess);
            }
        }

        void PauseOrResumeProcess(Process process)
        {
            var blacklistedProcesses = _localSettingsRepository.BlacklistedProcesses;

            if (blacklistedProcesses?.Select(processInfo => processInfo.Name).Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase) == true)
            {
                _pauseManager.PauseActivity(PauseReason.ActiveProcessBlacklisted, process.ProcessName);
            }
            else
            {
                _pauseManager.ResumeActivity(PauseReason.ActiveProcessBlacklisted);
            }
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            CheckActiveProcess();
        }

        /*
        private void OnFocusChangedHandler(object src, AutomationFocusChangedEventArgs args)
        {
            var element = src as AutomationElement;
            if (element == null)
            {
                return;
            }

            try
            {
                using (var process = Process.GetProcessById(element.Current.ProcessId))
                {
                    var processName = process.ProcessName;
                    if (processName == "explorer" || processName == "Mémoire")
                    {
                        return;
                    }

                    PauseOrResumeProcess(process);
                }
            }
            catch
            {
                // ignored
            }
        }*/
    }
}
