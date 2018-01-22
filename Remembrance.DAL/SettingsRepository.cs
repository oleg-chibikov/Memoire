using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class SettingsRepository : LiteDbRepository<Settings, int>, ISettingsRepository
    {
        [NotNull]
        protected override string DbName => nameof(Settings);

        [NotNull]
        protected override string DbPath => Paths.SharedDataPath;

        public Settings Get()
        {
            return Collection.FindById(1) ?? new Settings();
        }

        public void UpdateOrInsert(Settings settings)
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