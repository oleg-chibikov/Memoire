using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.Sync;
using Remembrance.Resources;
using Scar.Common;
using Scar.Common.DAL;
using Scar.Common.DAL.Model;
using Scar.Common.Messages;

namespace Remembrance.Core.Sync
{
    sealed class RepositorySynhronizer<TEntity, TId, TRepository> : IRepositorySynhronizer
        where TRepository : class, IRepository<TEntity, TId>, ITrackedRepository, IFileBasedRepository, IDisposable
        where TEntity : IEntity<TId>, ITrackedEntity
    {
        readonly IAutofacNamedInstancesFactory _autofacNamedInstancesFactory;

        readonly ILocalSettingsRepository _localSettingsRepository;

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        readonly TRepository _ownRepository;

        readonly IReadOnlyCollection<ISyncExtender<TRepository>> _syncExtenders;

        readonly ISyncPostProcessor<TEntity>? _syncPostProcessor;

        readonly ISyncPreProcessor<TEntity>? _syncPreProcessor;

        public RepositorySynhronizer(
            IAutofacNamedInstancesFactory autofacNamedInstancesFactory,
            ILogger<RepositorySynhronizer<TEntity, TId, TRepository>> logger,
            TRepository ownRepository,
            IMessageHub messageHub,
            ILocalSettingsRepository localSettingsRepository,
            IReadOnlyCollection<ISyncExtender<TRepository>> syncExtenders,
            ISyncPreProcessor<TEntity>? syncPreProcessor = null,
            ISyncPostProcessor<TEntity>? syncPostProcessor = null)
        {
            _ownRepository = ownRepository;
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _syncExtenders = syncExtenders ?? throw new ArgumentNullException(nameof(syncExtenders));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _autofacNamedInstancesFactory = autofacNamedInstancesFactory ?? throw new ArgumentNullException(nameof(autofacNamedInstancesFactory));
            _syncPreProcessor = syncPreProcessor;
            _syncPostProcessor = syncPostProcessor;
        }

        public string FileName => _ownRepository.DbFileName;

        public void SyncRepository(string filePath)
        {
            ApplyRemoteRepositoryAction(filePath, SyncInternal);
        }

        void ApplyRemoteRepositoryAction(string filePath, Action<TRepository> action)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            if (extension != _ownRepository.DbFileExtension)
            {
                throw new NotSupportedException($"Improper repository file extension: {filePath}");
            }

            // Copy is needed because LiteDB changes the remote file when creation a repository over it and it could lead to the conflicts.
            var newDirectoryPath = Path.GetTempPath();
            var newFilePath = Path.Combine(newDirectoryPath, fileName + extension);
            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
            }

            File.Copy(filePath, newFilePath);
            var parameters = new Parameter[]
            {
                new TypedParameter(typeof(string), newDirectoryPath),
                new TypedParameter(typeof(bool), false)
            };
            using (var remoteRepository = _autofacNamedInstancesFactory.GetInstance<TRepository>(parameters))
            {
                foreach (var syncExtender in _syncExtenders)
                {
                    syncExtender.OnSynchronizing(remoteRepository);
                }

                action(remoteRepository);
            }

            File.Delete(newFilePath);
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General catch for all types of exceptions")]
        void SyncInternal(TRepository remoteRepository)
        {
            var lastSyncedRecordModifiedTime = _localSettingsRepository.GetSyncTime(FileName);

            var changed = remoteRepository.GetModifiedAfter(lastSyncedRecordModifiedTime).Cast<TEntity>().ToArray();
            if (!changed.Any())
            {
                return;
            }

            var maxLastModified = changed.Max(x => x.ModifiedDate);
            Parallel.ForEach(
                changed,
                async remoteEntity =>
                {
                    try
                    {
                        // _logger.LogTrace("Processing {0}...", remoteEntity);
                        var existingEntity = _ownRepository.TryGetById(remoteEntity.Id);
                        var insert = false;
                        if (!Equals(existingEntity, default))
                        {
                            if (remoteEntity.ModifiedDate <= existingEntity.ModifiedDate)
                            {
                                return;
                            }
                        }
                        else
                        {
                            insert = true;
                        }

                        if (_syncPreProcessor != null)
                        {
                            if (!await _syncPreProcessor.BeforeEntityChangedAsync(existingEntity, remoteEntity).ConfigureAwait(false))
                            {
                                _logger.LogDebug("Preprocessor condition not satisfied for {0}", remoteEntity);
                                return;
                            }
                        }

                        if (insert)
                        {
                            _ownRepository.Insert(remoteEntity, true);
                            _logger.LogInformation("{0} inserted", remoteEntity);
                        }
                        else
                        {
                            _ownRepository.Update(remoteEntity, true);
                            _logger.LogInformation("{0} updated", remoteEntity);
                        }

                        if (_syncPostProcessor != null)
                        {
                            await _syncPostProcessor.AfterEntityChangedAsync(existingEntity, remoteEntity).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _messageHub.Publish(
                            string.Format(
                                    CultureInfo.InvariantCulture,
                                    Errors.CannotSynchronize,
                                    remoteEntity,
                                    Path.Combine(remoteRepository.DbDirectoryPath, $"{remoteRepository.DbFileName}{remoteRepository.DbFileExtension}"))
                                .ToError(ex));
                    }
                });

            _localSettingsRepository.AddOrUpdateSyncTime(FileName, maxLastModified);
        }
    }
}
