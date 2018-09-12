using System;
using System.Diagnostics;
using Remembrance.Contracts.Sync;
using Scar.Common.IO;

namespace Remembrance.Resources
{
    public static class ProcessCommands
    {
        public static void OpenSettingsFolder()
        {
            Process.Start($@"{CommonPaths.SettingsPath}");
        }

        public static void OpenSharedFolder(SyncBus syncBus)
        {
            if (syncBus == SyncBus.NoSync)
            {
                OpenSettingsFolder();
                return;
            }

            Process.Start($@"{RemembrancePaths.GetSharedPath(syncBus)}");
        }

        public static void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }
    }
}