using System;
using System.IO;
using JetBrains.Annotations;
using Remembrance.Contracts.Sync;
using Scar.Common.IO;

namespace Remembrance.Resources
{
    public static class RemembrancePaths
    {
        public static readonly string OneDrivePath = CommonPaths.GetOneDrivePath();

        public static readonly string DropBoxPath = CommonPaths.GetDropboxPath();

        [NotNull]
        public static string LocalSharedDataPath { get; } = Path.Combine(CommonPaths.SettingsPath, "Shared");

        [NotNull]
        public static string GetSharedPath(SyncBus syncBus)
        {
            var basePath = syncBus == SyncBus.OneDrive ? OneDrivePath : DropBoxPath;
            return Path.Combine(basePath, CommonPaths.ProgramName, Environment.MachineName.SanitizePath());
        }
    }
}