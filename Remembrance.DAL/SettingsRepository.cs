using Common.Logging;
using JetBrains.Annotations;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.DAL
{
    [UsedImplicitly]
    internal sealed class SettingsRepository : LiteDbRepository<Settings>, ISettingsRepository
    {
        public SettingsRepository([NotNull] ILog logger) : base(logger)
        {
        }

        protected override string DbName => nameof(Settings);

        protected override string TableName => nameof(Settings);

        public Settings Get()
        {
            return Db.GetCollection<Settings>(TableName).FindById(1) ?? new Settings();
        }
    }
}