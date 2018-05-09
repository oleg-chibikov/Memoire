using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ISharedRepository : ITrackedRepository, IFileBasedRepository, IChangeableRepository
    {
    }
}