using System;
using Mémoire.Contracts.DAL.Model;
using Newtonsoft.Json;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL
{
    abstract class BaseSettingsRepository : TrackedLiteDbRepository<ApplicationSettings, string>
    {
        protected BaseSettingsRepository(string directoryPath, string fileName, bool shrink = true) : base(directoryPath, fileName, shrink)
        {
        }

        protected void RemoveUpdateOrInsert<TValue>(string key, TValue value)
        {
            if (Equals(value, default))
            {
                Delete(key);
                return;
            }

            var settings = new ApplicationSettings { Id = key, ValueJson = JsonConvert.SerializeObject(value) };
            if (!Update(settings))
            {
                Insert(settings);
            }
        }

        protected TValue TryGetValue<TValue>(string key, Func<TValue> defaultValueProvider)
        {
            var entry = TryGetById(key);
            return entry == null ? defaultValueProvider() : JsonConvert.DeserializeObject<TValue>(entry.ValueJson);
        }

        protected TValue TryGetValue<TValue>(string key, TValue defaultValue = default)
        {
            var entry = TryGetById(key);
            return entry == null ? defaultValue : JsonConvert.DeserializeObject<TValue>(entry.ValueJson);
        }
    }
}
