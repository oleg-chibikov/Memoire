using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts
{
    public interface IPathsProvider
    {
        string? DropBoxPath { get; }

        string? OneDrivePath { get; }

        string LocalSharedDataPath { get; }

        string GetSharedPath(SyncEngine syncEngine);

        void OpenSettingsFolder();

        void OpenSharedFolder(SyncEngine syncEngine);

        void ViewLogs();
    }
}
