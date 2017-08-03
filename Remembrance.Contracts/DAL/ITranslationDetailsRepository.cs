using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL
{
    public interface ITranslationDetailsRepository : IRepository<TranslationDetails, int>
    {
        void DeleteByTranslationEntryId([NotNull] object translationEntryId);

        [CanBeNull]
        TranslationDetails TryGetByTranslationEntryId([NotNull] object translationEntryId);
    }
}