using System;
using System.Diagnostics;

namespace Remembrance.Resources
{
    public static class ProcessCommands
    {
        public static void OpenSettingsFolder()
        {
            Process.Start($@"{Paths.SettingsPath}");
        }

        public static void OpenSharedFolder()
        {
            Process.Start($@"{Paths.SharedDataPath}");
        }

        public static void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }
    }
}