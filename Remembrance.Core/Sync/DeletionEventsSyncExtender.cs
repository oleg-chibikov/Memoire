using System;
using System.Collections.Generic;
using System.Linq;
using Remembrance.Contracts.Sync;
using Scar.Common.DAL;
using Scar.Common.DAL.Model;

namespace Remembrance.Core.Sync
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

        public DeletionEventsSyncExtender(TDeletionEventRepository ownRepository)
        {
            _ownRepository = ownRepository ?? throw new ArgumentNullException(nameof(ownRepository));
            _ownDeletionEventsToClear = new List<TId>(_ownRepository.GetAll().Select(x => x.Id));
        }

        public void OnSynchronizationFinished()
        {
            lock (_locker)
            {
                _collectInfo = false;
                if (_ownDeletionEventsToClear.Any())
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
