using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Local
{
    public interface ILocalSettingsRepository : IRepository<LocalSettings, int>
    {
        [NotNull]
        LocalSettings Get();

        void UpdateOrInsert([NotNull] LocalSettings settings);
    }
}