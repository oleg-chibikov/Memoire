using System;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Sync;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scar.Common.DAL.Contracts.Model;

namespace Mémoire.Core.Sync
{
    public sealed class SettingsSyncPostProcessor : ISyncPostProcessor<ApplicationSettings>
    {
        readonly IMessageHub _messageHub;

        public SettingsSyncPostProcessor(IMessageHub messageHub, ILogger<SettingsSyncPostProcessor> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public Task AfterEntityChangedAsync(ApplicationSettings? oldValue, ApplicationSettings newValue)
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
