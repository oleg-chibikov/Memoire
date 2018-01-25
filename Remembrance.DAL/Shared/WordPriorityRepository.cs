using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
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
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.TranslationEntryId);
        }

        public IWord[] GetPriorityWordsForTranslationEntry(object translationEntryId)
        {
            return Collection.Find(x => x.Id.TranslationEntryId.Equals(translationEntryId))
                .Select(x => x.Id)
                .Cast<IWord>()
                .ToArray();
        }

        public void ClearForTranslationEntry(object translationEntryId)
        {
            Collection.Delete(x => x.Id.TranslationEntryId.Equals(translationEntryId));
        }
    }
}