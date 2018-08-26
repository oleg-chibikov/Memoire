using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.DAL.LiteDB;
using Scar.Common.IO;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails, TranslationEntryKey>, ITranslationDetailsRepository
    {
        public TranslationDetailsRepository()
            : base(CommonPaths.SettingsPath)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}