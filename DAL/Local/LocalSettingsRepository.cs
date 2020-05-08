using System;
using System.Collections.Generic;
using System.Threading;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.ApplicationLifetime.Contracts;
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
            nameof(ApplicationSettings))
        {
            _pathsProvider = pathsProvider ?? throw new ArgumentNullException(nameof(pathsProvider));
        }

        public AvailableLanguagesInfo? AvailableLanguages
        {
            get => TryGetValue<AvailableLanguagesInfo>(nameof(AvailableLanguages));
            set => RemoveUpdateOrInsert(nameof(AvailableLanguages), value);
        }

        public DateTime? AvailableLanguagesModifiedDate => TryGetById(nameof(AvailableLanguages))?.ModifiedDate;

        public bool IsActive
        {
            get => TryGetValue(nameof(IsActive), true);
            set => RemoveUpdateOrInsert(nameof(IsActive), (bool?)value);
        }

        public DateTime? LastCardShowTime
        {
            get => TryGetValue<DateTime?>(nameof(LastCardShowTime));
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

        public void AddOrUpdatePauseInfo(PauseReasons pauseReasons, PauseInfoCollection? pauseInfo)
        {
            RemoveUpdateOrInsert(PauseTimeKey + pauseReasons, pauseInfo);
        }

        public void AddOrUpdateSyncTime(string repository, DateTime syncTime)
        {
            RemoveUpdateOrInsert(SyncTimeKey + repository, syncTime);
        }

        public PauseInfoCollection GetPauseInfo(PauseReasons pauseReasons)
        {
            return TryGetValue(PauseTimeKey + pauseReasons, () => new PauseInfoCollection());
        }

        public DateTime GetSyncTime(string repository)
        {
            return TryGetValue(SyncTimeKey + repository, DateTime.MinValue);
        }
    }
}