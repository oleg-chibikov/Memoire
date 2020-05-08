using System;
using System.Collections.Generic;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Sync;

namespace Remembrance.Contracts.DAL.Local
{
    public interface ILocalSettingsRepository
    {
        IReadOnlyCollection<ProcessInfo>? BlacklistedProcesses { get; set; }

        AvailableLanguagesInfo? AvailableLanguages { get; set; }

        DateTime? AvailableLanguagesModifiedDate { get; }

        bool IsActive { get; set; }

        DateTime? LastCardShowTime { get; set; }

        string? LastUsedSourceLanguage { get; set; }

        string? LastUsedTargetLanguage { get; set; }

        SyncEngine SyncEngine { get; set; }

        string UiLanguage { get; set; }

        void AddOrUpdatePauseInfo(PauseReasons pauseReasons, PauseInfoCollection? pauseInfo);

        void AddOrUpdateSyncTime(string repository, DateTime syncTime);

        PauseInfoCollection GetPauseInfo(PauseReasons pauseReasons);

        DateTime GetSyncTime(string repository);
    }
}
