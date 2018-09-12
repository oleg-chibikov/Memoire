using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
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
        private readonly ISharedRepositoryPathsProvider _sharedRepositoryPathsProvider;

        [NotNull]
        private readonly IReadOnlyCollection<ISyncExtender> _syncExtenders;

        [NotNull]
        private readonly IDictionary<string, IRepositorySynhronizer> _synchronizers;

        public SynchronizationManager(
            [NotNull] ILog logger,
            [NotNull] IReadOnlyCollection<IRepositorySynhronizer> synchronizers,
            [NotNull] ISharedRepositoryPathsProvider sharedRepositoryPathsProvider,
            [NotNull] IReadOnlyCollection<ISyncExtender> syncExtenders)
        {
            _ = synchronizers ?? throw new ArgumentNullException(nameof(synchronizers));
            if (!synchronizers.Any())
            {
                throw new ArgumentNullException(nameof(synchronizers));
            }

            _synchronizers = synchronizers.ToDictionary(x => x.FileName, x => x);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sharedRepositoryPathsProvider = sharedRepositoryPathsProvider ?? throw new ArgumentNullException(nameof(sharedRepositoryPathsProvider));
            _syncExtenders = syncExtenders ?? throw new ArgumentNullException(nameof(syncExtenders));
            SynchronizeExistingRepositories();
            _fileSystemWatcher = new FileSystemWatcher(sharedRepositoryPathsProvider.BaseDirectoryPath)
            {
                IncludeSubdirectories = true,
                InternalBufferSize = 64 * 1024,
                NotifyFilter = NotifyFilters.FileName
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

        private void FileSystemWatcher_Changed(object sender, [NotNull] FileSystemEventArgs e)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            var directoryPath = Path.GetDirectoryName(e.FullPath);
            if (directoryPath == null || directoryPath == RemembrancePaths.SharedDataPath)
            {
                return;
            }

            _logger.InfoFormat("File system event received: {0}: {1}, {2}", e.ChangeType, e.Name, e.FullPath);
            SynchronizeFile(e.FullPath);
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void SynchronizeExistingRepositories()
        {
            var paths = _sharedRepositoryPathsProvider.GetSharedRepositoriesPaths();
            Parallel.ForEach(
                paths,
                filePath =>
                {
                    _logger.TraceFormat("Processing file {0}...", filePath);
                    SynchronizeFile(filePath);
                });
            foreach (var syncExtender in _syncExtenders)
            {
                syncExtender.OnSynchronizationFinished();
            }
        }

        private void SynchronizeFile([NotNull] string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            if (!_synchronizers.ContainsKey(fileName))
            {
                _logger.WarnFormat("Unknown type of repository: {0}", filePath);
                return;
            }

            _synchronizers[fileName].SyncRepository(filePath);
        }
    }
}