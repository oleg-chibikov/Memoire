using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface ITranslationEntryRepository : IRepository<TranslationEntry>
    {
        [CanBeNull]
        TranslationEntry GetCurrent();

        [CanBeNull]
        TranslationEntry TryGetByKey([NotNull] TranslationEntryKey key);
    }
}