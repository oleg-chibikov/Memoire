using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails>, ITranslationDetailsRepository
    {
        internal const string DictionaryDbName = "Dictionary";

        public TranslationDetailsRepository([NotNull] ILog logger)
            : base(logger)
        {
        }

        protected override string DbName => DictionaryDbName;

        protected override string TableName => nameof(TranslationDetails);
    }
}