using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL.Contracts
{
    public interface ISettingsRepository : IRepository<Settings>
    {
        [NotNull]
        Settings Get();
    }
}