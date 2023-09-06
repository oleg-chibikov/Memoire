using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Sync;
using Microsoft.Extensions.Logging;

namespace Mémoire.Core.Sync
{
    public sealed class TranslationEntrySyncPostProcessor : ISyncPostProcessor<TranslationEntry>
    {
        readonly IMessageHub _messageHub;

        public TranslationEntrySyncPostProcessor(IMessageHub messageHub, ILogger<TranslationEntrySyncPostProcessor> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public Task AfterEntityChangedAsync(TranslationEntry oldValue, TranslationEntry newValue)
        {
            _ = newValue ?? throw new ArgumentNullException(nameof(newValue));
            _messageHub.Publish(newValue);
            return Task.CompletedTask;
        }
    }
}
