using Remembrance.Contracts.Sync;

namespace Remembrance.Contracts
{
    public interface IRemembrancePathsProvider
    {
        string DropBoxPath { get; }
        string LocalSharedDataPath { get; }
        string OneDrivePath { get; }

        string GetSharedPath(SyncBus syncBus);
        void OpenSettingsFolder();
        void OpenSharedFolder(SyncBus syncBus);
        void ViewLogs();
    }
}