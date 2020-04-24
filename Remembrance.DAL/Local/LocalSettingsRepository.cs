using System;
using System.Collections.Generic;
using System.Threading;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Sync;
using Scar.Common.ApplicationLifetime.Contracts;

namespace Remembrance.DAL.Local
{
    sealed class LocalSettingsRepository : BaseSettingsRepository, ILocalSettingsRepository
    {
        const string PauseTimeKey = "PauseTime_";

        const string SyncTimeKey = "SyncTime_";

        readonly IRemembrancePathsProvider _remembrancePathsProvider;

        public LocalSettingsRepository(IRemembrancePathsProvider remembrancePathsProvider, IAssemblyInfoProvider assemblyInfoProvider) : base(
            assemblyInfoProvider?.SettingsPath ?? throw new ArgumentNullException(nameof(assemblyInfoProvider)),
            nameof(ApplicationSettings))
        {
            _remembrancePathsProvider = remembrancePathsProvider ?? throw new ArgumentNullException(nameof(remembrancePathsProvider));
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
                    () => _remembrancePathsProvider.DropBoxPath != null ? SyncEngine.DropBox : _remembrancePathsProvider.OneDrivePath != null ? SyncEngine.OneDrive : SyncEngine.NoSync);
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
                        var newUiLanguage = (uiLanguage == Constants.EnLanguage) || (uiLanguage == Constants.RuLanguage) ? uiLanguage : Constants.EnLanguage;

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
