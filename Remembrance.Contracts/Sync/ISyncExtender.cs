using Scar.Common.DAL;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncExtender
    {
        void OnSynchronizationFinished();
    }

    public interface ISyncExtender<in TRepository> : ISyncExtender
        where TRepository : ITrackedRepository
    {
        void OnSynchronizing(TRepository remoteRepository);
    }
}
