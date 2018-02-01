using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class WordPriorityRepository : TrackedLiteDbRepository<WordPriority, WordKey>, IWordPriorityRepository
    {
        public WordPriorityRepository([CanBeNull] string directoryPath = null, [CanBeNull] string fileName = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, fileName, shrink)
        {
            Collection.EnsureIndex(x => x.Id.WordText);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id);
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }

        public IWord[] GetPriorityWordsForTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            return Collection.Find(x => x.Id.Text == translationEntryKey.Text && x.Id.SourceLanguage == translationEntryKey.SourceLanguage && x.Id.TargetLanguage == translationEntryKey.TargetLanguage)
                .Select(x => x.Id)
                .Cast<IWord>()
                .ToArray();
        }

        public void ClearForTranslationEntry(TranslationEntryKey translationEntryKey)
        {
            Collection.Delete(x => x.Id.Text == translationEntryKey.Text && x.Id.SourceLanguage == translationEntryKey.SourceLanguage && x.Id.TargetLanguage == translationEntryKey.TargetLanguage);
        }
    }
}