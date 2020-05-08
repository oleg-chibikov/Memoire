using System;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL.SharedBetweenMachines
{
    sealed class TranslationEntryDeletionRepository : TrackedLiteDbRepository<TranslationEntryDeletion, TranslationEntryKey>, ITranslationEntryDeletionRepository
    {
        public TranslationEntryDeletionRepository(IPathsProvider pathsProvider, string? directoryPath = null, bool shrink = true) : base(
            directoryPath ?? pathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(pathsProvider)),
            null,
            shrink)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}
