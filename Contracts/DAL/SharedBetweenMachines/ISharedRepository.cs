using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.SharedBetweenMachines
{
    public interface ISharedRepository : ITrackedRepository, IFileBasedRepository, IChangeableRepository
    {
    }
}
