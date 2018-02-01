using System;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class SettingsRepository : TrackedLiteDbRepository<Settings, int>, ISettingsRepository
    {
        public SettingsRepository([CanBeNull] string directoryPath, [CanBeNull] string fileName = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, fileName, shrink)
        {
        }

        public SettingsRepository([CanBeNull] string directoryPath = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, null, shrink)
        {
        }

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