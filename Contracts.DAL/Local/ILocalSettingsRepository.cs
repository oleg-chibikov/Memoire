using System;
using System.Collections.Generic;
using Mémoire.Contracts.DAL.Model;

namespace Mémoire.Contracts.DAL.Local
{
    public interface ILocalSettingsRepository
    {
        IReadOnlyCollection<ProcessInfo>? BlacklistedProcesses { get; set; }

        AvailableLanguagesInfo? AvailableLanguages { get; set; }

        DateTimeOffset? AvailableLanguagesModifiedDate { get; }

        bool IsActive { get; set; }

        DateTimeOffset? LastCardShowTime { get; set; }

        string? LastUsedSourceLanguage { get; set; }

        string? LastUsedTargetLanguage { get; set; }

        SyncEngine SyncEngine { get; set; }

        string UiLanguage { get; set; }

        void AddOrUpdatePauseInfo(PauseReasons pauseReasons, PauseInfoCollection? pauseInfo);

        void AddOrUpdateSyncTime(string repository, DateTimeOffset syncTime);

        PauseInfoCollection GetPauseInfo(PauseReasons pauseReasons);

        DateTimeOffset GetSyncTime(string repository);
    }
}
