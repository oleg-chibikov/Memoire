using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Remembrance.DAL
{
    internal static class DbPathResolver
    {
        //TODO: evaluate settings path only once during installation
        //TODO: change settings path feature
        [NotNull]
        public static string ResolvePath()
        {
            string programName = $"Scar\\{nameof(Remembrance)}";
            var path = Path.Combine(GetDropboxPath() ?? GetOneDrivePath() ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), programName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

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