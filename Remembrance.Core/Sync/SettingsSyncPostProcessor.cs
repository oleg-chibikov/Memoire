using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    [UsedImplicitly]
    internal sealed class SettingsSyncPostProcessor : ISyncPostProcessor<Settings>
    {
        [NotNull]
        private readonly IMessageHub _messageHub;

        public SettingsSyncPostProcessor([NotNull] IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public Task AfterEntityChangedAsync(Settings oldValue, Settings newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException(nameof(newValue));
            }

            var prevFreq = oldValue?.CardShowFrequency;
            var newFreq = newValue.CardShowFrequency;

            if (prevFreq != newFreq)
            {
                _messageHub.Publish(newFreq);
            }

            return Task.CompletedTask;
        }
    }
}