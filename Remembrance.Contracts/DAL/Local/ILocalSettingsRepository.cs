using System;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;

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

        [CanBeNull]
        AvailableLanguagesInfo AvailableLanguages { get; set; }

        DateTime? AvailableLanguagesModifiedDate { get; }

        void AddOrUpdatePauseInfo(PauseReason pauseReason, [CanBeNull] PauseInfoCollection pauseInfo);

        void AddOrUpdateSyncTime([NotNull] string repository, DateTime syncTime);

        [NotNull]
        PauseInfoCollection GetPauseInfo(PauseReason pauseReason);

        DateTime GetSyncTime([NotNull] string repository);
    }
}