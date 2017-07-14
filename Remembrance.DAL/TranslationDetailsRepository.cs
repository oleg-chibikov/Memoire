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
        internal const string DictionaryDbName = "Dictionary";

        public TranslationDetailsRepository([NotNull] ILog logger)
            : base(logger)
        {
        }

        [NotNull]
        protected override string DbName => DictionaryDbName;

        [NotNull]
        protected override string DbPath => Paths.SettingsPath;
    }
}