using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class PrepositionsInfoRepository : LiteDbRepository<PrepositionsInfo, int>, IPrepositionsInfoRepository
    {
        public PrepositionsInfoRepository()
        {
            Collection.EnsureIndex(x => x.TranslationEntryId);
        }

        [NotNull]
        protected override string DbName => nameof(PrepositionsInfo);

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;

        public PrepositionsInfo GetPrepositionsInfo(object translationEntryId)
        {
            return Collection.FindOne(x => x.TranslationEntryId.Equals(translationEntryId));
        }

        public bool CheckPrepositionsInfoExists(object translationEntryId)
        {
            return Collection.Exists(x => x.TranslationEntryId.Equals(translationEntryId));
        }
    }
}