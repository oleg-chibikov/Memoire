using JetBrains.Annotations;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails>, ITranslationDetailsRepository
    {
        public TranslationDetailsRepository()
            : base(Paths.SettingsPath)
        {
        }
    }
}