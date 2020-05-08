using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Sync;

namespace Remembrance.Core.Sync
{
    sealed class SettingsSyncPostProcessor : ISyncPostProcessor<ApplicationSettings>
    {
        readonly IMessageHub _messageHub;

        public SettingsSyncPostProcessor(IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public Task AfterEntityChangedAsync(ApplicationSettings oldValue, ApplicationSettings newValue)
        {
            _ = newValue ?? throw new ArgumentNullException(nameof(newValue));
            if (newValue.Id != nameof(ISharedSettingsRepository.CardShowFrequency))
            {
                return Task.CompletedTask;
            }

            var prevFreq = oldValue == null ? TimeSpan.MinValue : JsonConvert.DeserializeObject<TimeSpan>(oldValue.ValueJson);
            var newFreq = JsonConvert.DeserializeObject<TimeSpan>(newValue.ValueJson);

            if (prevFreq != newFreq)
            {
                _messageHub.Publish(newFreq);
            }

            return Task.CompletedTask;
        }
    }
}
