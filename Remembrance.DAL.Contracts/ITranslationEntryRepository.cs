using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL.Contracts
{
    public interface ITranslationEntryRepository : IRepository<TranslationEntry>
    {
        [CanBeNull]
        TranslationEntry GetCurrent();

        [CanBeNull]
        TranslationEntry TryGetByKey([NotNull] TranslationEntryKey key);
    }
}