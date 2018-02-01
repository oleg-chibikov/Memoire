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
            _fileSystemWatcher.Created += _fileSystemWatcher_Changed;
            _fileSystemWatcher.Changed += _fileSystemWatcher_Changed;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _fileSystemWatcher.Created -= _fileSystemWatcher_Changed;
            _fileSystemWatcher.Changed -= _fileSystemWatcher_Changed;
            _fileSystemWatcher.Dispose();
        }

        private void SynchronizeExistingRepositories([NotNull] string rootDirectoryPath)
        {
            foreach (var directoryPath in Directory.GetDirectories(rootDirectoryPath))
            {
                if (directoryPath == Paths.SharedDataPath)
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(directoryPath))
                {
                    _logger.TraceFormat("Processing file {0}", filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    SynchronizeFile(directoryPath, fileName);
                }
            }
        }

        private void _fileSystemWatcher_Changed(object sender, [NotNull] FileSystemEventArgs e)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            var directoryPath = Path.GetDirectoryName(e.FullPath);
            if (directoryPath == null || directoryPath == Paths.SharedDataPath)
            {
                return;
            }

            var fileName = Path.GetFileNameWithoutExtension(e.Name);

            _logger.InfoFormat("File system event received: {0}: {1}, {2}", e.ChangeType, e.Name, e.FullPath);
            SynchronizeFile(directoryPath, fileName);
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void SynchronizeFile([NotNull] string directoryPath, [NotNull] string fileName)
        {
            //TODO: Handle deleted entities (separate repository for them)
            //TODO: What if the same entity was updated here and remotely? Merge conflict?
            //TODO: Treat item renaming as deletion and then insertion?
            if (!_synchronizers.ContainsKey(fileName))
            {
                throw new NotSupportedException($"Unknown type of repository: {fileName}");
            }

            _synchronizers[fileName].SyncRepository(directoryPath);
        }
    }
}