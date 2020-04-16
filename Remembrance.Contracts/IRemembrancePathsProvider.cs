using Remembrance.Contracts.Sync;

namespace Remembrance.Contracts
{
    public interface IRemembrancePathsProvider
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
