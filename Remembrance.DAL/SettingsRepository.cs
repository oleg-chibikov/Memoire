using Common.Logging;
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
        public SettingsRepository([NotNull] ILog logger)
            : base(logger)
        {
            Collection.EnsureIndex(x => x.Id, true);
        }

        [NotNull]
        protected override string DbName => nameof(Settings);

        [NotNull]
        protected override string DbPath => Paths.SharedDataPath;

        public Settings Get()
        {
            return Collection.FindById(1) ?? new Settings();
        }
    }
}