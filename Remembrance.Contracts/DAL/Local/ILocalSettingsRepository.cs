using System;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;

namespace Remembrance.Contracts.DAL.Local
{
    public interface ILocalSettingsRepository
    {
        bool IsActive { get; set; }

        DateTime? LastCardShowTime { get; set; }

        [CanBeNull]
        string LastUsedSourceLanguage { get; set; }

        [CanBeNull]
        string LastUsedTargetLanguage { get; set; }

        [NotNull]
        string UiLanguage { get; set; }

        void AddOrUpdateSyncTime([NotNull] string repository, DateTimeOffset syncTime);

        DateTimeOffset GetSyncTime([NotNull] string repository);

        void AddOrUpdatePauseInfo(PauseReason pauseReason, [CanBeNull] PauseInfoCollection pauseInfo);

        [NotNull]
        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);
    }
}