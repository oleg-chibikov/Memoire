using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails>, ITranslationDetailsRepository
    {
        public TranslationDetailsRepository()
        {
            Collection.EnsureIndex(x => x.Id, true);
        }

        [NotNull]
        protected override string DbName => nameof(TranslationDetails);

        /// <remarks>
        /// Not shared folder - if details are missing - they are re-downloaded
        /// </remarks>
        [NotNull]
        protected override string DbPath => Paths.SettingsPath;
    }
}