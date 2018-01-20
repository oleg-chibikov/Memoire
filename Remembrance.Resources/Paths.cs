using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Remembrance.Resources
{
    public static class Paths
    {
        // TODO: Library
        [NotNull]
        private static readonly Regex IllegalCharactersRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]", RegexOptions.Compiled);

        [NotNull]
        private static readonly string ProgramName = $"{((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false)).Company}\\{nameof(Remembrance)}";

        [NotNull]
        public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProgramName);

        [CanBeNull]
        private static readonly string BaseSharedDataPath = GetDropboxPath() ?? GetOneDrivePath();

        [NotNull]
        public static readonly string SharedDataPath = BaseSharedDataPath == null
            ? SettingsPath
            : Path.Combine(BaseSharedDataPath, ProgramName, SanitizePath(Environment.MachineName));

        // TODO: Move to IO Library
        // TODO: Settings and Installer - add to choose which program to use
        [CanBeNull]
        private static string GetDropboxPath()
        {
            const string infoPath = @"Dropbox\info.json";

            var jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), infoPath);
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), infoPath);
            }

            return !File.Exists(jsonPath)
                ? null
                : File.ReadAllText(jsonPath)
                    .Split('\"')[5]
                    .Replace(@"\\", @"\");
        }

        [CanBeNull]
        private static string GetOneDrivePath()
        {
            const string notDetected = "ND";
            var path = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\OneDrive", "UserFolder", notDetected);
            return path == notDetected || string.IsNullOrWhiteSpace(path)
                ? null
                : path;
        }

        [NotNull]
        private static string SanitizePath([NotNull] this string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return IllegalCharactersRegex.Replace(path, string.Empty);
        }
    }
}