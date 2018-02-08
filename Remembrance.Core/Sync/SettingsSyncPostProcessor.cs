using System;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    internal sealed class SettingsSyncPostProcessor : ISyncPostProcessor<Settings>
    {
        [NotNull]
        private readonly IMessageHub _messenger;

        public SettingsSyncPostProcessor([NotNull] IMessageHub messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public void OnEntityChanged(Settings oldValue, Settings newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException(nameof(newValue));
            }

            var prevFreq = oldValue?.CardShowFrequency;
            var newFreq = newValue.CardShowFrequency;

            if (prevFreq != newFreq)
            {
                _messenger.Publish(newFreq);
            }
        }
    }
}