using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class PrepositionsInfoRepository : LiteDbRepository<PrepositionsInfo>, IPrepositionsInfoRepository
    {
        public PrepositionsInfoRepository()
        {
            Collection.EnsureIndex(x => x.Id, true);
        }

        [NotNull]
        protected override string DbName => nameof(PrepositionsInfo);

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;
    }
}