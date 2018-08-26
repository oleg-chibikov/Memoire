using System;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.IO;
using Scar.Common.WPF.Localization;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class LocalSettingsRepository : BaseSettingsRepository, ILocalSettingsRepository
    {
        [NotNull]
        private const string PauseTimeKey = "PauseTime_";

        [NotNull]
        private const string SyncTimeKey = "SyncTime_";

        public LocalSettingsRepository()
            : base(CommonPaths.SettingsPath, nameof(Settings))
        {
        }

        public AvailableLanguagesInfo AvailableLanguages
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

        public string LastUsedSourceLanguage
        {
            get => TryGetValue<string>(nameof(LastUsedSourceLanguage));
            set => RemoveUpdateOrInsert(nameof(LastUsedSourceLanguage), value);
        }

        public string LastUsedTargetLanguage
        {
            get => TryGetValue<string>(nameof(LastUsedTargetLanguage));
            set => RemoveUpdateOrInsert(nameof(LastUsedTargetLanguage), value);
        }

        public string UiLanguage
        {
            get =>
                TryGetValue(
                    nameof(UiLanguage),
                    () =>
                    {
                        var uiLanguage = CultureUtilities.GetCurrentCulture().Name;
                        if (uiLanguage == Constants.EnLanguage || uiLanguage == Constants.RuLanguage)
                        {
                            return uiLanguage;
                        }

                        return Constants.EnLanguage;
                    });

            set => RemoveUpdateOrInsert(nameof(UiLanguage), value);
        }

        public void AddOrUpdatePauseInfo(PauseReason pauseReason, PauseInfoCollection pauseInfo)
        {
            RemoveUpdateOrInsert(PauseTimeKey + pauseReason, pauseInfo);
        }

        public void AddOrUpdateSyncTime(string repository, DateTime syncTime)
        {
            RemoveUpdateOrInsert(SyncTimeKey + repository, syncTime);
        }

        public PauseInfoCollection GetPauseInfo(PauseReason pauseReason)
        {
            return TryGetValue(PauseTimeKey + pauseReason, () => new PauseInfoCollection());
        }

        public DateTime GetSyncTime(string repository)
        {
            return TryGetValue(SyncTimeKey + repository, DateTime.MinValue);
        }
    }
}