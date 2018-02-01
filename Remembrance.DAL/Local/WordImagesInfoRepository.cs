using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
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
            Collection.EnsureIndex(x => x.Id);
            Collection.EnsureIndex(x => x.Id.WordText);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }

        public void ClearForTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            Collection.Delete(x => x.Id.Text == translationEntryKey.Text && x.Id.SourceLanguage == translationEntryKey.SourceLanguage && x.Id.TargetLanguage == translationEntryKey.TargetLanguage);
        }
    }
}