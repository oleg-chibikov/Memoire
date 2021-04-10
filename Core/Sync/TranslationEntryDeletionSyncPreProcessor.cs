using System;
using System.Threading.Tasks;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Sync;
using Microsoft.Extensions.Logging;

namespace Mémoire.Core.Sync
{
    sealed class TranslationEntryDeletionSyncPreProcessor : ISyncPreProcessor<TranslationEntryDeletion>
    {
        readonly ITranslationEntryProcessor _translationEntryProcessor;

        public TranslationEntryDeletionSyncPreProcessor(ITranslationEntryProcessor translationEntryProcessor, ILogger<TranslationEntryDeletionSyncPreProcessor> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        public Task<bool> BeforeEntityChangedAsync(TranslationEntryDeletion oldValue, TranslationEntryDeletion newValue)
        {
            _ = newValue ?? throw new ArgumentNullException(nameof(newValue));
            _translationEntryProcessor.DeleteTranslationEntry(newValue.Id, false);
            return Task.FromResult(false);
        }
    }
}
