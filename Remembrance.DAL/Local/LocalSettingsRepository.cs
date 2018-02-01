using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class LocalSettingsRepository : LiteDbRepository<LocalSettings, int>, ILocalSettingsRepository
    {
        public LocalSettingsRepository()
            : base(Paths.SettingsPath)
        {
        }

        public LocalSettings Get()
        {
            return Collection.FindById(1) ?? new LocalSettings();
        }

        public void UpdateOrInsert(LocalSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (!Update(settings))
            {
                Insert(settings);
            }
        }
    }
}