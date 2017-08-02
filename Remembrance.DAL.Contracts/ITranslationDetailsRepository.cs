using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.DAL;

namespace Remembrance.DAL.Contracts
{
    public interface ITranslationDetailsRepository : IRepository<TranslationDetails, int>
    {
        void DeleteByTranslationEntryId([NotNull] object translationEntryId);

        [CanBeNull]
        TranslationDetails TryGetByTranslationEntryId([NotNull] object translationEntryId);
    }
}