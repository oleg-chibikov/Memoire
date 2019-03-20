using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.Sync;
using Scar.Common.IO;
using Scar.Common.Sync;

namespace Remembrance.Core
{
    public class RemembrancePathsProvider : IRemembrancePathsProvider
    {
        public RemembrancePathsProvider(IOneDrivePathProvider oneDrivePathProvider, IDropBoxPathProvider dropBoxPathProvider)
        {
            _ = oneDrivePathProvider ?? throw new ArgumentNullException(nameof(oneDrivePathProvider));
            _ = dropBoxPathProvider ?? throw new ArgumentNullException(nameof(dropBoxPathProvider));
            OneDrivePath = oneDrivePathProvider.GetOneDrivePath();
            DropBoxPath = dropBoxPathProvider.GetDropBoxPath();
        }

        public string? OneDrivePath { get; }

        public string? DropBoxPath { get; }

        [NotNull]
        public string LocalSharedDataPath { get; } = Path.Combine(CommonPaths.SettingsPath, "Shared");

        [NotNull]
        public string GetSharedPath(SyncBus syncBus)
        {
            var basePath = syncBus == SyncBus.OneDrive ? OneDrivePath : DropBoxPath;
            return Path.Combine(basePath, CommonPaths.ProgramName, Environment.MachineName.SanitizePath());
        }

        public void OpenSettingsFolder()
        {
            Process.Start($@"{CommonPaths.SettingsPath}");
        }

        public void OpenSharedFolder(SyncBus syncBus)
        {
            if (syncBus == SyncBus.NoSync)
            {
                OpenSettingsFolder();
                return;
            }

            Process.Start($@"{GetSharedPath(syncBus)}");
        }

        public void ViewLogs()
        {
            Process.Start($@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }
    }
}