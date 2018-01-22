using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class WordImagesInfoRepository : LiteDbRepository<WordImageInfo, WordKey>, IWordImagesInfoRepository
    {
        public WordImagesInfoRepository()
        {
            Collection.EnsureIndex(x => x.Id, true);
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.TranslationEntryId);
        }

        [NotNull]
        protected override string DbName => nameof(WordImageInfo);

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;
    }
}