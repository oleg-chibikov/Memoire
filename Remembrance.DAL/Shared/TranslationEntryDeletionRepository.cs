using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class TranslationEntryDeletionRepository : TrackedLiteDbRepository<TranslationEntryDeletion, TranslationEntryKey>, ITranslationEntryDeletionRepository
    {
        public TranslationEntryDeletionRepository([CanBeNull] string directoryPath = null, bool shrink = true)
            : base(directoryPath ?? RemembrancePaths.LocalSharedDataPath, null, shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}