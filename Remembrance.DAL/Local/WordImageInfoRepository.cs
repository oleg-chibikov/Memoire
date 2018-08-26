using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL.LiteDB;
using Scar.Common.IO;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class WordImageInfoRepository : LiteDbRepository<WordImageInfo, WordKey>, IWordImageInfoRepository
    {
        public WordImageInfoRepository()
            : base(CommonPaths.SettingsPath)
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