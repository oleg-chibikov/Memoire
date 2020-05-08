using Scar.Common.DAL.Contracts;

namespace Mémoire.Contracts.DAL.SharedBetweenMachines
{
    public interface ISharedRepository : ITrackedRepository, IFileBasedRepository, IChangeableRepository
    {
    }
}
