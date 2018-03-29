using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL;

namespace Remembrance.DAL.Local
{
    internal static class SettingsRepositoryExtensions
    {
        public static void RemoveUpdateOrInsert<T, TSettings>([NotNull] this IRepository<TSettings, string> repository, [NotNull] string key, [CanBeNull] T value)
            where TSettings : ISettings, new()
        {
            if (Equals(value, default(T)))
            {
                repository.Delete(key);
                return;
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            repository.UpdateOrInsert(key, value);
        }

        public static void UpdateOrInsert<T, TSettings>([NotNull] this IRepository<TSettings, string> repository, [NotNull] string key, [NotNull] T value)
            where TSettings : ISettings, new()
        {
            var settings = new TSettings
            {
                Id = key,
                Value = value
            };
            if (!repository.Update(settings))
            {
                repository.Insert(settings);
            }
        }
    }
}