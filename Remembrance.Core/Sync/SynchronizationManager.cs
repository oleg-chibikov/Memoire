using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class SynchronizationManager : ISynchronizationManager, IDisposable
    {
        [NotNull]
        private readonly FileSystemWatcher _fileSystemWatcher;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IDictionary<string, IRepositorySynhronizer> _synchronizers;

        public SynchronizationManager(
            [NotNull] ILog logger,
            [NotNull] INamedInstancesFactory namedInstancesFactory,
            [NotNull] IMessageHub messageHub,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IRepositorySynhronizer[] synhronizers)
        {
            if (synhronizers == null)
            {
                throw new ArgumentNullException(nameof(synhronizers));
            }

            _synchronizers = synhronizers.ToDictionary(x => x.FileName, x => x);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var commonSharedFolder = Directory.GetParent(Paths.SharedDataPath);
            SynchronizeExistingRepositories(commonSharedFolder.FullName);
            _fileSystemWatcher = new FileSystemWatcher(commonSharedFolder.FullName)
            {
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024,
                Filter = "*.db"
            };
            _fileSystemWatcher.Created += FileSystemWatcher_Changed;
            _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _fileSystemWatcher.Created -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Dispose();
        }

        private void SynchronizeExistingRepositories([NotNull] string rootDirectoryPath)
        {
            foreach (var filePath in Directory.GetDirectories(rootDirectoryPath).Where(directoryPath => directoryPath != Paths.SharedDataPath).SelectMany(Directory.GetFiles))
            {
                _logger.TraceFormat("Processing file {0}...", filePath);
                SynchronizeFile(filePath);
            }
        }

        private void FileSystemWatcher_Changed(object sender, [NotNull] FileSystemEventArgs e)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            var directoryPath = Path.GetDirectoryName(e.FullPath);
            if (directoryPath == null || directoryPath == Paths.SharedDataPath)
            {
                return;
            }
            _logger.InfoFormat("File system event received: {0}: {1}, {2}", e.ChangeType, e.Name, e.FullPath);
            SynchronizeFile(e.FullPath);
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void SynchronizeFile([NotNull] string fullPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(fullPath);
            var extension = Path.GetExtension(fullPath);
            //TODO: Handle deleted entities (separate repository for them)

            if (!_synchronizers.ContainsKey(fileName))
            {
                throw new NotSupportedException($"Unknown type of repository: {fileName}");
            }

            //Copy is needed because LiteDB changes the remote file when creation a repository over it and it could lead to the conflicts.
            var newDirectoryPath = Path.GetTempPath();
            var newFileName = Path.Combine(newDirectoryPath, fileName + extension);
            if (File.Exists(newFileName))
            {
                File.Delete(newFileName);
            }
            File.Copy(fullPath, newFileName);

            _synchronizers[fileName].SyncRepository(newDirectoryPath);

            File.Delete(newFileName);
        }
    }
}