using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class TranslationEntrySyncPostProcessor : ISyncPostProcessor<TranslationEntry>
    {
        [NotNull]
        private readonly IMessageHub _messageHub;

        public TranslationEntrySyncPostProcessor([NotNull] IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public Task AfterEntityChangedAsync(TranslationEntry oldValue, TranslationEntry newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException(nameof(newValue));
            }

            _messageHub.Publish(newValue);
            return Task.CompletedTask;
        }
    }
}