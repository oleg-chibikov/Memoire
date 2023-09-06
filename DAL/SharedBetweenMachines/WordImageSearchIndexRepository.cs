using System;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL.SharedBetweenMachines
{
    public sealed class WordImageSearchIndexRepository : TrackedLiteDbRepository<WordImageSearchIndex, WordKey>, IWordImageSearchIndexRepository
    {
        public WordImageSearchIndexRepository(IPathsProvider pathsProvider, string? directoryPath = null, bool shrink = false) : base(
            directoryPath ?? pathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(pathsProvider)),
            null,
            shrink)
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
                x => (x.Id.Key.Text == translationEntryKey.Text) &&
                     (x.Id.Key.SourceLanguage == translationEntryKey.SourceLanguage) &&
                     (x.Id.Key.TargetLanguage == translationEntryKey.TargetLanguage));
        }
    }
}
