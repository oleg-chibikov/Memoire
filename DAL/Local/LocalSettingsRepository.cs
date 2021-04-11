using System;
using System.Collections.Generic;
using System.Threading;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
using Scar.Common.DAL.Contracts.Model;
using Scar.Common.DAL.LiteDB;
using Scar.Services.Contracts.Data;

namespace Mémoire.DAL.Local
{
    sealed class LocalSettingsRepository : BaseSettingsRepository, ILocalSettingsRepository
    {
        const string PauseTimeKey = "PauseTime_";
        const string SyncTimeKey = "SyncTime_";
        readonly IPathsProvider _pathsProvider;

        public LocalSettingsRepository(IPathsProvider pathsProvider, IAssemblyInfoProvider assemblyInfoProvider) : base(
            assemblyInfoProvider?.SettingsPath ?? throw new ArgumentNullException(nameof(assemblyInfoProvider)),
            nameof(ApplicationSettings),
            shrink: false)
        {
            _pathsProvider = pathsProvider ?? throw new ArgumentNullException(nameof(pathsProvider));
        }

        public AvailableLanguagesInfo? AvailableLanguages
        {
            get => TryGetValue<AvailableLanguagesInfo>(nameof(AvailableLanguages));
            set => RemoveUpdateOrInsert(nameof(AvailableLanguages), value);
        }

        public DateTimeOffset? AvailableLanguagesModifiedDate => TryGetById(nameof(AvailableLanguages))?.ModifiedDate;

        public bool IsActive
        {
            get => TryGetValue(nameof(IsActive), true);
            set => RemoveUpdateOrInsert(nameof(IsActive), (bool?)value);
        }

        public DateTimeOffset? LastCardShowTime
        {
            get => TryGetValue<DateTimeOffset?>(nameof(LastCardShowTime));
            set => RemoveUpdateOrInsert(nameof(LastCardShowTime), value);
        }

        public string? LastUsedSourceLanguage
        {
            get => TryGetValue<string>(nameof(LastUsedSourceLanguage));
            set => RemoveUpdateOrInsert(nameof(LastUsedSourceLanguage), value);
        }

        public string? LastUsedTargetLanguage
        {
            get => TryGetValue<string>(nameof(LastUsedTargetLanguage));
            set => RemoveUpdateOrInsert(nameof(LastUsedTargetLanguage), value);
        }

        public SyncEngine SyncEngine
        {
            get =>
                TryGetValue(
                    nameof(SyncEngine),
                    () => _pathsProvider.DropBoxPath != null ? SyncEngine.DropBox : _pathsProvider.OneDrivePath != null ? SyncEngine.OneDrive : SyncEngine.NoSync);
            set => RemoveUpdateOrInsert(nameof(SyncEngine), (SyncEngine?)value);
        }

        public IReadOnlyCollection<ProcessInfo>? BlacklistedProcesses
        {
            get => TryGetValue<IReadOnlyCollection<ProcessInfo>>(nameof(BlacklistedProcesses));
            set => RemoveUpdateOrInsert(nameof(BlacklistedProcesses), value);
        }

        public string UiLanguage
        {
            get =>
                TryGetValue(
                    nameof(UiLanguage),
                    () =>
                    {
                        var uiLanguage = Thread.CurrentThread.CurrentUICulture.Name;
                        var newUiLanguage = (uiLanguage == LanguageConstants.EnLanguage) || (uiLanguage == LanguageConstants.RuLanguage) ? uiLanguage : LanguageConstants.EnLanguage;

                        RemoveUpdateOrInsert(nameof(UiLanguage), newUiLanguage);
                        return newUiLanguage;
                    });
            set => RemoveUpdateOrInsert(nameof(UiLanguage), value);
        }

        public void AddOrUpdatePauseInfo(PauseReasons pauseReasons, PauseInfoSummary? pauseInfo)
        {
            RemoveUpdateOrInsert(PauseTimeKey + pauseReasons, pauseInfo);
        }

        public void AddOrUpdateSyncTime(string repository, DateTimeOffset syncTime)
        {
            RemoveUpdateOrInsert(SyncTimeKey + repository, syncTime);
        }

        public PauseInfoSummary GetPauseInfo(PauseReasons pauseReasons)
        {
            return TryGetValue(PauseTimeKey + pauseReasons, () => new PauseInfoSummary());
        }

        public DateTimeOffset GetSyncTime(string repository)
        {
            return TryGetValue(SyncTimeKey + repository, DateTimeOffset.MinValue);
        }
    }
}
