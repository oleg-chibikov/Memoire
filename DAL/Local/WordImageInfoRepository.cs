using System;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL.Local
{
    public sealed class WordImageInfoRepository : LiteDbRepository<WordImageInfo, WordKey>, IWordImageInfoRepository
    {
        public WordImageInfoRepository(IAssemblyInfoProvider assemblyInfoProvider) : base(
            assemblyInfoProvider?.SettingsPath ?? throw new ArgumentNullException(nameof(assemblyInfoProvider)),
            shrink: false)
        {
            Collection.EnsureIndex(x => x.Id.Word.Text);
            Collection.EnsureIndex(x => x.Id.Word.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.Key.Text);
            Collection.EnsureIndex(x => x.Id.Key.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.Key.TargetLanguage);
        }

        public void ClearForTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            Collection.DeleteMany(
                x => (x.Id.Key.Text == translationEntryKey.Text) && (x.Id.Key.SourceLanguage == translationEntryKey.SourceLanguage) && (x.Id.Key.TargetLanguage == translationEntryKey.TargetLanguage));
        }
    }
}
