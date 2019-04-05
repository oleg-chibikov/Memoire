using System;
using System.Diagnostics;
using System.IO;
using Remembrance.Contracts;
using Remembrance.Contracts.Sync;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.IO;
using Scar.Common.Sync;

namespace Remembrance.Core
{
    public class RemembrancePathsProvider : IRemembrancePathsProvider
    {
        private readonly IAssemblyInfoProvider _assemblyInfoProvider;

        public RemembrancePathsProvider(IOneDrivePathProvider oneDrivePathProvider,
            IDropBoxPathProvider dropBoxPathProvider, IAssemblyInfoProvider assemblyInfoProvider)
        {
            _assemblyInfoProvider = assemblyInfoProvider ?? throw new ArgumentNullException(nameof(assemblyInfoProvider));
            _ = oneDrivePathProvider ?? throw new ArgumentNullException(nameof(oneDrivePathProvider));
            _ = dropBoxPathProvider ?? throw new ArgumentNullException(nameof(dropBoxPathProvider));
            OneDrivePath = oneDrivePathProvider.GetOneDrivePath();
            DropBoxPath = dropBoxPathProvider.GetDropBoxPath();
            LocalSharedDataPath = Path.Combine(_assemblyInfoProvider.SettingsPath, "Shared");
        }

        public string? OneDrivePath { get; }

        public string? DropBoxPath { get; }

        public string LocalSharedDataPath { get; }

        public string GetSharedPath(SyncBus syncBus)
        {
            var basePath = syncBus == SyncBus.OneDrive ? OneDrivePath : DropBoxPath;
            return Path.Combine(basePath, _assemblyInfoProvider.ProgramName, Environment.MachineName.SanitizePath());
        }

        public void OpenSettingsFolder()
        {
            Process.Start($@"{_assemblyInfoProvider.SettingsPath}");
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
            Process.Start(
                $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Remembrance\Logs\Full.log");
        }
    }
}