using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class TranslationEntryDeletionSyncPreProcessor : ISyncPreProcessor<TranslationEntryDeletion>
    {
        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        public TranslationEntryDeletionSyncPreProcessor([NotNull] ITranslationEntryProcessor translationEntryProcessor)
        {
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        public Task<bool> BeforeEntityChangedAsync(TranslationEntryDeletion oldValue, TranslationEntryDeletion newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException(nameof(newValue));
            }

            _translationEntryProcessor.DeleteTranslationEntry(newValue.Id, false);
            return Task.FromResult(false);
        }
    }
}