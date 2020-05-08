using System;
using System.Threading.Tasks;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Sync;

namespace Mémoire.Core.Sync
{
    sealed class TranslationEntryDeletionSyncPreProcessor : ISyncPreProcessor<TranslationEntryDeletion>
    {
        readonly ITranslationEntryProcessor _translationEntryProcessor;

        public TranslationEntryDeletionSyncPreProcessor(ITranslationEntryProcessor translationEntryProcessor)
        {
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        public Task<bool> BeforeEntityChangedAsync(TranslationEntryDeletion oldValue, TranslationEntryDeletion newValue)
        {
            _ = newValue ?? throw new ArgumentNullException(nameof(newValue));
            _translationEntryProcessor.DeleteTranslationEntry(newValue.Id, false);
            return Task.FromResult(false);
        }
    }
}
