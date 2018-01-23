using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface IWordPriorityRepository : IRepository<WordPriority, WordKey>
    {
        [NotNull]
        IWord[] GetPriorityWordsForTranslationEntry([NotNull] object translationEntryId);

        void ClearForTranslationEntry([NotNull] object translationEntryId);
    }
}