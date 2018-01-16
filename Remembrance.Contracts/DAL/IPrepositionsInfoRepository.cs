using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface IPrepositionsInfoRepository : IRepository<PrepositionsInfo, int>
    {
        [CanBeNull]
        PrepositionsInfo GetPrepositionsInfo([NotNull] object translationEntryId);

        bool CheckPrepositionsInfoExists([NotNull] object translationEntryId);
    }
}