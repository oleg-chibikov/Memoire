using System;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Local
{
    sealed class TranslationDetailsRepository : LiteDbRepository<TranslationDetails, TranslationEntryKey>, ITranslationDetailsRepository
    {
        public TranslationDetailsRepository(IAssemblyInfoProvider assemblyInfoProvider)
            : base(assemblyInfoProvider?.SettingsPath ?? throw new ArgumentNullException(nameof(assemblyInfoProvider)))
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}