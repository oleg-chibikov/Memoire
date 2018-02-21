using Scar.Common.DAL;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncExtender
    {
        void OnSynchronizationFinished();
    }

    public interface ISyncExtender<TEntity, TId, in TRepository> : ISyncExtender
        where TEntity : IEntity<TId>, ITrackedEntity
        where TRepository : ITrackedRepository<TEntity, TId>
    {
        void OnSynchronizing(TRepository remoteRepository);
    }
}