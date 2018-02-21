using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ISettingsRepository : ITrackedRepository<Settings, int>
    {
        [NotNull]
        Settings Get();

        void UpdateOrInsert([NotNull] Settings settings);
    }
}