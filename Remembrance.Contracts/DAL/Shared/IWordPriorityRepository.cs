using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface IWordPriorityRepository : ITrackedRepository<WordPriority, WordKey>, ISharedRepository
    {
        [NotNull]
        IWord[] GetPriorityWordsForTranslationEntry([NotNull] TranslationEntryKey translationEntryKey);

        void ClearForTranslationEntry([NotNull] TranslationEntryKey translationEntryKey);
    }
}