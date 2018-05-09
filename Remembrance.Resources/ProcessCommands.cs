using System;
using System.Diagnostics;
using Scar.Common.IO;

namespace Remembrance.Resources
{
    public static class ProcessCommands
    {
        public static void OpenSettingsFolder()
        {
            Process.Start($@"{CommonPaths.SettingsPath}");
        }

        public static void OpenSharedFolder()
        {
            Process.Start($@"{RemembrancePaths.SharedDataPath}");
        }

        public static void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }
    }
}