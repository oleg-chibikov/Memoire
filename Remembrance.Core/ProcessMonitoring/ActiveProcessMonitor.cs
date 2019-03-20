using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.ProcessMonitoring;

namespace Remembrance.Core.ProcessMonitoring
{
    [UsedImplicitly]
    internal sealed class ActiveProcessMonitor : IActiveProcessMonitor, IDisposable
    {
        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        [NotNull]
        private readonly Timer _timer;

        public ActiveProcessMonitor([NotNull] IPauseManager pauseManager, [NotNull] ILocalSettingsRepository localSettingsRepository)
        {
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            CheckActiveProcess();

            // Adding additional mechanizm to check active process since Automation can be unresponsive sometimes
            _timer = new Timer(1000);
            _timer.Start();
            _timer.Elapsed += Timer_Tick;

            // Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
        }

        public void Dispose()
        {
            // Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChangedHandler);
            _timer.Elapsed -= Timer_Tick;
            _timer.Stop();
            _timer.Dispose();
        }

        [CanBeNull]
        private static Process? GetActiveProcess()
        {
            var hwnd = GetForegroundWindow();
            return GetProcessByHandle(hwnd);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [CanBeNull]
        private static Process? GetProcessByHandle(IntPtr hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out var processId);
                return Process.GetProcessById((int)processId);
            }
            catch
            {
                return null;
            }
        }

        [DllImport("user32.dll")]

        // ReSharper disable once StyleCop.SA1305
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private void CheckActiveProcess()
        {
            var activeProcess = GetActiveProcess();
            if (activeProcess != null)
            {
                PauseOrResumeProcess(activeProcess);
            }
        }

        private void PauseOrResumeProcess(Process process)
        {
            var blacklistedProcesses = _localSettingsRepository.BlacklistedProcesses;

            if (blacklistedProcesses?.Select(processInfo => processInfo.Name).Contains(process.ProcessName, StringComparer.InvariantCultureIgnoreCase) == true)
            {
                _pauseManager.Pause(PauseReason.ActiveProcessBlacklisted, process.ProcessName);
            }
            else
            {
                _pauseManager.Resume(PauseReason.ActiveProcessBlacklisted);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
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
                    if (processName == "explorer" || processName == "Remembrance")
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