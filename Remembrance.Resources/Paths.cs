using System;
using System.IO;
using JetBrains.Annotations;
using Scar.Common.IO;

namespace Remembrance.Resources
{
    public static class RemembrancePaths
    {
        static RemembrancePaths()
        {
            var baseSharedPath = CommonPaths.GetDropboxPath() ?? CommonPaths.GetOneDrivePath();
            SharedDataPath = baseSharedPath == null ? CommonPaths.SettingsPath : Path.Combine(baseSharedPath, CommonPaths.ProgramName, Environment.MachineName.SanitizePath());
        }

        [NotNull]
        public static string LocalSharedDataPath { get; } = Path.Combine(CommonPaths.SettingsPath, "Shared");

        [NotNull]
        public static string SharedDataPath { get; }
    }
}