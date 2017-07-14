using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.DAL;

namespace Remembrance.DAL.Contracts
{
    public interface ISettingsRepository : IRepository<Settings, int>
    {
        [NotNull]
        Settings Get();
    }
}