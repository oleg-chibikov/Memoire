using System;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Remembrance.Resources
{
    public static class Paths
    {
        [NotNull]
        private static readonly string ProgramName = $"{((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false)).Company}\\{nameof(Remembrance)}";

        [NotNull]
        public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProgramName);

        [CanBeNull]
        private static readonly string BaseSharedDataPath = GetDropboxPath() ?? GetOneDrivePath();

        [CanBeNull]
        public static readonly string SharedDataPath = BaseSharedDataPath == null ? null : Path.Combine(BaseSharedDataPath, ProgramName);

        [CanBeNull]
        private static string GetOneDrivePath()
        {
            const string notDetected = "ND";
            var path = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\OneDrive", "UserFolder", notDetected);
            return path == notDetected || string.IsNullOrWhiteSpace(path) ? null : path;
        }

        [CanBeNull]
        private static string GetDropboxPath()
        {
            const string infoPath = @"Dropbox\info.json";

            var jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), infoPath);
            if (!File.Exists(jsonPath))
                jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), infoPath);
            return !File.Exists(jsonPath) ? null : File.ReadAllText(jsonPath).Split('\"')[5].Replace(@"\\", @"\");
        }
    }
}