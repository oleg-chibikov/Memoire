using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL
{
    public interface IWordPriorityRepository
    {
        [NotNull]
        IWord[] GetPriorityWordsForTranslationEntry([NotNull] object translationEntryId);

        bool IsPriority([NotNull] IWord word, [NotNull] object translationEntryId);

        void MarkNonPriority([NotNull] IWord word, [NotNull] object translationEntryId);

        void MarkPriority([NotNull] IWord word, [NotNull] object translationEntryId);
    }
}