using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.DAL;

namespace Remembrance.DAL.Contracts
{
    public interface ITranslationEntryRepository : IRepository<TranslationEntry, int>
    {
        [CanBeNull]
        TranslationEntry GetCurrent();

        [CanBeNull]
        TranslationEntry TryGetByKey([NotNull] TranslationEntryKey key);
    }
}