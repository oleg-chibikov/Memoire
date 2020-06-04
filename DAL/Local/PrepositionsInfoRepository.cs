using System;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.DAL.LiteDB;

namespace Mémoire.DAL.Local
{
    sealed class PrepositionsInfoRepository : LiteDbRepository<PrepositionsInfo, TranslationEntryKey>, IPrepositionsInfoRepository
    {
        public PrepositionsInfoRepository(IAssemblyInfoProvider assemblyInfoProvider) : base(
            assemblyInfoProvider?.SettingsPath ?? throw new ArgumentNullException(nameof(assemblyInfoProvider)),
            shrink: false)
        {
            Collection.EnsureIndex(x => x.Id.Text);
            Collection.EnsureIndex(x => x.Id.SourceLanguage);
            Collection.EnsureIndex(x => x.Id.TargetLanguage);
        }
    }
}
