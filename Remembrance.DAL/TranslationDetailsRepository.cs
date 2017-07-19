using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails, int>, ITranslationDetailsRepository
    {
        public TranslationDetailsRepository([NotNull] ILog logger)
            : base(logger)
        {
        }

        [NotNull]
        protected override string DbName => "TranslationDetails";

        /// <remarks>
        /// Not shared folder - if details are missing - they are re-downloaded
        /// </remarks>
        [NotNull]
        protected override string DbPath => Paths.SettingsPath;
    }
}