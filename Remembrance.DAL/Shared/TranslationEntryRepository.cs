using System;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    sealed class TranslationEntryRepository : TrackedLiteDbRepository<TranslationEntry, TranslationEntryKey>, ITranslationEntryRepository
    {
        public TranslationEntryRepository(IRemembrancePathsProvider remembrancePathsProvider, string? directoryPath = null, bool shrink = true)
            : base(directoryPath ?? remembrancePathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(remembrancePathsProvider)), null, shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}