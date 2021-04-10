using System;
using System.Collections.Generic;
using System.Linq;
using Mémoire.Contracts.Sync;
using Microsoft.Extensions.Logging;
using Scar.Common.DAL.Contracts;
using Scar.Common.DAL.Contracts.Model;

namespace Mémoire.Core.Sync
{
    sealed class DeletionEventsSyncExtender<TEntity, TDeletionEntity, TId, TRepository, TDeletionEventRepository> : ISyncExtender<TRepository>
        where TEntity : IEntity<TId>
        where TDeletionEntity : IEntity<TId>, ITrackedEntity
        where TRepository : IRepository<TEntity, TId>, ITrackedRepository
        where TDeletionEventRepository : class, IRepository<TDeletionEntity, TId>
    {
        readonly object _locker = new object();
        readonly IList<TId> _ownDeletionEventsToClear;
        readonly TDeletionEventRepository _ownRepository;
        bool _collectInfo = true;

        public DeletionEventsSyncExtender(TDeletionEventRepository ownRepository, ILogger<DeletionEventsSyncExtender<TEntity, TDeletionEntity, TId, TRepository, TDeletionEventRepository>> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            _ownRepository = ownRepository ?? throw new ArgumentNullException(nameof(ownRepository));
            _ownDeletionEventsToClear = new List<TId>(_ownRepository.GetAll().Select(x => x.Id));
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        public void OnSynchronizationFinished()
        {
            lock (_locker)
            {
                _collectInfo = false;
                if (_ownDeletionEventsToClear.Count > 0)
                {
                    _ownRepository.Delete(_ownDeletionEventsToClear);
                }
            }
        }

        public void OnSynchronizing(TRepository remoteRepository)
        {
            if (!_collectInfo)
            {
                return;
            }

            lock (_locker)
            {
                if (!_collectInfo)
                {
                    return;
                }

                var existInRemoteRepository = _ownDeletionEventsToClear.Where(remoteRepository.Check).ToArray();
                foreach (var translationEntryKey in existInRemoteRepository)
                {
                    _ownDeletionEventsToClear.Remove(translationEntryKey);
                }
            }
        }
    }
}
