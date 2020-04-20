using System;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.SharedBetweenMachines
{
    sealed class WordImageSearchIndexRepository : TrackedLiteDbRepository<WordImageSearchIndex, WordKey>, IWordImageSearchIndexRepository
    {
        public WordImageSearchIndexRepository(IRemembrancePathsProvider remembrancePathsProvider, string? directoryPath = null, bool shrink = true) : base(
            directoryPath ?? remembrancePathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(remembrancePathsProvider)),
            null,
            shrink)
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
                x => (x.Id.TranslationEntryKey.Text == translationEntryKey.Text) &&
                     (x.Id.TranslationEntryKey.SourceLanguage == translationEntryKey.SourceLanguage) &&
                     (x.Id.TranslationEntryKey.TargetLanguage == translationEntryKey.TargetLanguage));
        }
    }
}
