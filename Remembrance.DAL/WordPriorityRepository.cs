using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class WordPriorityRepository : LiteDbRepository<WordPriority, WordKey>, IWordPriorityRepository
    {
        public WordPriorityRepository()
        {
            Collection.EnsureIndex(x => x.Id, true);
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.PartOfSpeech);
            Collection.EnsureIndex(x => x.Id.TranslationEntryId);
        }

        [NotNull]
        protected override string DbName => nameof(WordPriority);

        [NotNull]
        protected override string DbPath => Paths.SharedDataPath;

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