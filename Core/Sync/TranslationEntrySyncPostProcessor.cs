using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Sync;

namespace Mémoire.Core.Sync
{
    sealed class TranslationEntrySyncPostProcessor : ISyncPostProcessor<TranslationEntry>
    {
        readonly IMessageHub _messageHub;

        public TranslationEntrySyncPostProcessor(IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public Task AfterEntityChangedAsync(TranslationEntry oldValue, TranslationEntry newValue)
        {
            _ = newValue ?? throw new ArgumentNullException(nameof(newValue));
            _messageHub.Publish(newValue);
            return Task.CompletedTask;
        }
    }
}
