using System;
using System.IO;
using System.Linq;
using Autofac;
using Autofac.Core;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class SynchronizationManager : ISynchronizationManager, IDisposable
    {
        [NotNull]
        private readonly FileSystemWatcher _fileSystemWatcher;
        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;
        [NotNull]
        private readonly IWordPriorityRepository _wordPriorityRepository;
        [NotNull]
        private readonly ISettingsRepository _settingsRepository;
        [NotNull]
        private readonly INamedInstancesFactory _namedInstancesFactory;
        [NotNull]
        private readonly ILog _logger;
        [NotNull]
        private readonly IMessageHub _messageHub;
        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        public SynchronizationManager(
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] IWordPriorityRepository wordPriorityRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] INamedInstancesFactory namedInstancesFactory,
            [NotNull] IMessageHub messageHub,
            [NotNull] IViewModelAdapter viewModelAdapter)
        {
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _wordPriorityRepository = wordPriorityRepository ?? throw new ArgumentNullException(nameof(wordPriorityRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _namedInstancesFactory = namedInstancesFactory ?? throw new ArgumentNullException(nameof(namedInstancesFactory));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
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
            try
            {
                var parameters = new Parameter[]
                {
                    new PositionalParameter(0, directoryPath),
                    new TypedParameter(typeof(bool), false)
                };
                if (fileName == _translationEntryRepository.DbFileName)
                {
                    using (var repository = _namedInstancesFactory.GetInstance<ITranslationEntryRepository>(parameters))
                    {
                    }
                }
                else if (fileName == _wordPriorityRepository.DbFileName)
                {
                    using (var repository = _namedInstancesFactory.GetInstance<IWordPriorityRepository>(parameters))
                    {
                    }
                }
                //TODO: LocalSettings: Store dictionary: Filename-LastSyncDate, LastCardShowTime
                else if (fileName == _settingsRepository.DbFileName)
                {
                    using (var repository = _namedInstancesFactory.GetInstance<ISettingsRepository>(parameters))
                    {
                        var currentSettings = _settingsRepository.Get();
                        var remoteSettings = repository.GetModifiedAfter(currentSettings.ModifiedDate)
                            .SingleOrDefault();
                        if (remoteSettings != null)
                        {
                            _logger.Info("Updating settings...");
                            _viewModelAdapter.Adapt(remoteSettings, currentSettings);
                            _settingsRepository.UpdateOrInsert(currentSettings);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _messageHub.Publish($"Cannot synchronize {directoryPath}\\{fileName}".ToError(ex));
            }
        }

        public void Dispose()
        {
            _fileSystemWatcher.Created -= _fileSystemWatcher_Changed;
            _fileSystemWatcher.Changed -= _fileSystemWatcher_Changed;
            _fileSystemWatcher.Dispose();
        }
    }
}