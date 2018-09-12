using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Sync;

namespace Remembrance.Contracts.DAL.Local
{
    public interface ILocalSettingsRepository
    {
        [CanBeNull]
        IReadOnlyCollection<ProcessInfo> BlacklistedProcesses { get; set; }

        [CanBeNull]
        AvailableLanguagesInfo AvailableLanguages { get; set; }

        DateTime? AvailableLanguagesModifiedDate { get; }

        bool IsActive { get; set; }

        DateTime? LastCardShowTime { get; set; }

        [CanBeNull]
        string LastUsedSourceLanguage { get; set; }

        [CanBeNull]
        string LastUsedTargetLanguage { get; set; }

        SyncBus SyncBus { get; set; }

        [NotNull]
        string UiLanguage { get; set; }

        void AddOrUpdatePauseInfo(PauseReason pauseReason, [CanBeNull] PauseInfoCollection pauseInfo);

        void AddOrUpdateSyncTime([NotNull] string repository, DateTime syncTime);

        [NotNull]
        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        DateTime GetSyncTime([NotNull] string repository);
    }
}