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

        SyncBus SyncBus { get; set; }

        string UiLanguage { get; set; }

        void AddOrUpdatePauseInfo(PauseReason pauseReason, PauseInfoCollection? pauseInfo);

        void AddOrUpdateSyncTime(string repository, DateTime syncTime);

        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        DateTime GetSyncTime(string repository);
    }
}