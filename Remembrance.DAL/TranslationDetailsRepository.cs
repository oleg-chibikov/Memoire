using System;
using Common.Logging;
using JetBrains.Annotations;
using LiteDB;
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
            Collection.EnsureIndex(x => x.TranslationEntryId, true);
        }

        [NotNull]
        protected override string DbName => "TranslationDetails";

        /// <remarks>
        /// Not shared folder - if details are missing - they are re-downloaded
        /// </remarks>
        [NotNull]
        protected override string DbPath => Paths.SettingsPath;

        public TranslationDetails TryGetByTranslationEntryId(object translationEntryId)
        {
            if (translationEntryId == null)
                throw new ArgumentNullException(nameof(translationEntryId));

            return Collection.FindOne(Query.EQ(nameof(TranslationDetails.TranslationEntryId), new BsonValue(translationEntryId)));
        }

        public void DeleteByTranslationEntryId(object translationEntryId)
        {
            if (translationEntryId == null)
                throw new ArgumentNullException(nameof(translationEntryId));

            Collection.Delete(Query.EQ(nameof(TranslationDetails.TranslationEntryId), new BsonValue(translationEntryId)));
        }
    }
}