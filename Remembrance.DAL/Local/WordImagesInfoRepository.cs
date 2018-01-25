using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class WordImagesInfoRepository : LiteDbRepository<WordImageInfo, WordKey>, IWordImagesInfoRepository
    {
        public WordImagesInfoRepository()
            : base(Paths.SettingsPath)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.TranslationEntryId);
        }
    }
}