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
            Collection.EnsureIndex(x => x.Id.Word.Text);
            Collection.EnsureIndex(x => x.Id.Word.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.TranslationEntryKey.Text);
            Collection.EnsureIndex(x => x.Id.TranslationEntryKey.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TranslationEntryKey.TargetLanguage);
        }

        public void ClearForTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            Collection.Delete(
                x => x.Id.TranslationEntryKey.Text == translationEntryKey.Text
                     && x.Id.TranslationEntryKey.SourceLanguage == translationEntryKey.SourceLanguage
                     && x.Id.TranslationEntryKey.TargetLanguage == translationEntryKey.TargetLanguage);
        }
    }
}