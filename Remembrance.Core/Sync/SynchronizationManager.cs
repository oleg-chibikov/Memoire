using System.IO;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;

namespace Remembrance.Core.Sync
{
    internal sealed class SynchronizationManager : ISynchronizationManager
    {
        private FileSystemWatcher _fileSystemWatcher;

        public SynchronizationManager()
        {
            var commonSharedFolder = Directory.GetParent(Paths.SharedDataPath);
            _fileSystemWatcher = new FileSystemWatcher(commonSharedFolder.FullName)
            {
                IncludeSubdirectories = true
            };
        }
    }
}