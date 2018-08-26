using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal abstract class BaseSettingsRepository : TrackedLiteDbRepository<Settings, string>
    {
        protected BaseSettingsRepository([NotNull] string directoryPath, [NotNull] string fileName, bool shrink = true)
            : base(directoryPath, fileName, shrink)
        {
        }

        protected void RemoveUpdateOrInsert<TValue>(string key, TValue value)
        {
            if (Equals(value, default(TValue)))
            {
                Delete(key);
                return;
            }

            var settings = new Settings
            {
                Id = key,
                ValueJson = JsonConvert.SerializeObject(value)
            };
            if (!Update(settings))
            {
                Insert(settings);
            }
        }

        protected TValue TryGetValue<TValue>([NotNull] string key, Func<TValue> defaultValueProvider)
        {
            var entry = TryGetById(key);
            return entry == null ? defaultValueProvider() : JsonConvert.DeserializeObject<TValue>(entry.ValueJson);
        }

        protected TValue TryGetValue<TValue>([NotNull] string key, TValue defaultValue = default(TValue))
        {
            var entry = TryGetById(key);
            return entry == null ? defaultValue : JsonConvert.DeserializeObject<TValue>(entry.ValueJson);
        }
    }
}