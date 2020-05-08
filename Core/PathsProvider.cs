using System;
using System.IO;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.IO;
using Scar.Common.Sync.Contracts;

namespace Mémoire.Core
{
    public class PathsProvider : IPathsProvider
    {
        readonly IAssemblyInfoProvider _assemblyInfoProvider;

        public PathsProvider(IOneDrivePathProvider oneDrivePathProvider, IDropBoxPathProvider dropBoxPathProvider, IAssemblyInfoProvider assemblyInfoProvider)
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

        public string GetSharedPath(SyncEngine syncEngine)
        {
            var basePath = syncEngine == SyncEngine.OneDrive ? OneDrivePath : DropBoxPath;
            return Path.Combine(basePath, _assemblyInfoProvider.ProgramName, Environment.MachineName.SanitizePath());
        }

        public void OpenSettingsFolder()
        {
            $@"{_assemblyInfoProvider.SettingsPath}".OpenDirectoryInExplorer();
        }

        public void OpenSharedFolder(SyncEngine syncEngine)
        {
            if (syncEngine == SyncEngine.NoSync)
            {
                OpenSettingsFolder();
                return;
            }

            $@"{GetSharedPath(syncEngine)}".OpenDirectoryInExplorer();
        }

        public void ViewLogs()
        {
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\Scar\Mémoire\Logs\Full.log".OpenPathWithDefaultAction();
        }
    }
}
