using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    internal sealed class TranslationEntrySyncPostProcessor : ISyncPostProcessor<TranslationEntry>
    {
        private readonly IMessageHub _messageHub;

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