using JetBrains.Annotations;
using Remembrance.DAL.Contracts.Model;
using Scar.Common.DAL;

namespace Remembrance.DAL.Contracts
{
    public interface ITranslationDetailsRepository : IRepository<TranslationDetails, int>
    {
        bool CheckByTranslationEntryId([NotNull] object translationEntryId);

        void DeleteByTranslationEntryId([NotNull] object translationEntryId);

        [NotNull]
        TranslationDetails GetByTranslationEntryId([NotNull] object translationEntryId);
    }
}