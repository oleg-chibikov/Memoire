using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;
using Scar.Common.DAL;
using Scar.Common.DAL.Model;
using Scar.Common.Messages;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class RepositorySynhronizer<T, TId, TRepository> : IRepositorySynhronizer
        where TRepository : ITrackedRepository<T, TId>
        where T : IEntity<TId>, ITrackedEntity
    {
        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly INamedInstancesFactory _namedInstancesFactory;

        [NotNull]
        private readonly TRepository _ownRepository;

        [CanBeNull]
        private readonly ISyncPostProcessor<T> _syncPostProcessor;

        public RepositorySynhronizer(
            [NotNull] INamedInstancesFactory namedInstancesFactory,
            [NotNull] ILog logger,
            [NotNull] TRepository ownRepository,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [CanBeNull] ISyncPostProcessor<T> syncPostProcessor = null)
        {
            _ownRepository = ownRepository;
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _namedInstancesFactory = namedInstancesFactory ?? throw new ArgumentNullException(nameof(namedInstancesFactory));
            _syncPostProcessor = syncPostProcessor;
        }

        public string FileName => _ownRepository.DbFileName;

        public void SyncRepository(string directoryPath)
        {
            if (directoryPath == null)
            {
                throw new ArgumentNullException(nameof(directoryPath));
            }

            var parameters = new Parameter[]
            {
                new PositionalParameter(0, directoryPath),
                new TypedParameter(typeof(bool), false)
            };
            using (var remoteRepository = _namedInstancesFactory.GetInstance<TRepository>(parameters))
            {
                var filePath = Path.Combine(directoryPath, FileName);
                var localSettings = _localSettingsRepository.Get();
                if (!localSettings.SyncTimes.TryGetValue(filePath, out var lastSyncTime))
                {
                    lastSyncTime = DateTimeOffset.MinValue;
                }

                var changed = remoteRepository.GetModifiedAfter(lastSyncTime);
                Parallel.ForEach(
                    changed,
                    async remoteEntity =>
                    {
                        try
                        {
                            _logger.TraceFormat("Processing {0}...", remoteEntity);
                            var existingEntity = _ownRepository.TryGetById(remoteEntity.Id);
                            if (!Equals(existingEntity, default(T)))
                            {
                                if (remoteEntity.ModifiedDate <= existingEntity.ModifiedDate)
                                {
                                    _logger.DebugFormat("Existing entity {0} is newer than the remote one {1}", existingEntity, remoteEntity);
                                    return;
                                }

                                _ownRepository.Update(remoteEntity);
                                _logger.InfoFormat("{0} updated", remoteEntity);
                            }
                            else
                            {
                                _ownRepository.Insert(remoteEntity);
                                _logger.InfoFormat("{0} inserted", remoteEntity);
                            }

                            if (_syncPostProcessor != null)
                            {
                                await _syncPostProcessor.OnEntityChangedAsync(existingEntity, remoteEntity).ConfigureAwait(false);
                            }

                            localSettings.SyncTimes[FileName] = DateTimeOffset.UtcNow;
                            _localSettingsRepository.UpdateOrInsert(localSettings);
                        }
                        catch (Exception ex)
                        {
                            _messageHub.Publish(string.Format(Errors.CannotSynchronize, remoteEntity, filePath).ToError(ex));
                        }
                    });
            }
        }
    }
}