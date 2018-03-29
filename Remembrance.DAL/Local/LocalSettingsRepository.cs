using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;
using Scar.Common.WPF.Localization;

namespace Remembrance.DAL.Local
{
    [UsedImplicitly]
    internal sealed class LocalSettingsRepository : LiteDbRepository<LocalSettings, string>, ILocalSettingsRepository
    {
        [NotNull]
        private const string SyncTimeKey = "SyncTime_";

        [NotNull]
        private const string PauseTimeKey = "PauseTime_";

        public LocalSettingsRepository()
            : base(Paths.SettingsPath)
        {
        }

        public bool IsActive
        {
            get
            {
                var obj = TryGetById(nameof(IsActive));
                return (bool?)obj?.Value ?? true;
            }

            set => this.UpdateOrInsert(nameof(IsActive), value);
        }

        public DateTime? LastCardShowTime
        {
            get
            {
                var obj = TryGetById(nameof(LastCardShowTime));
                return (DateTime?)obj?.Value;
            }

            set => this.RemoveUpdateOrInsert(nameof(LastCardShowTime), value);
        }

        public string LastUsedSourceLanguage
        {
            get => (string)TryGetById(nameof(LastUsedSourceLanguage))?.Value;
            set => this.RemoveUpdateOrInsert(nameof(LastUsedSourceLanguage), value);
        }

        public string LastUsedTargetLanguage
        {
            get => (string)TryGetById(nameof(LastUsedTargetLanguage))?.Value;
            set => this.RemoveUpdateOrInsert(nameof(LastUsedTargetLanguage), value);
        }

        public string UiLanguage
        {
            get => (string)TryGetById(nameof(UiLanguage))?.Value ?? CultureUtilities.GetCurrentCulture().ToString();
            set => this.RemoveUpdateOrInsert(nameof(UiLanguage), value);
        }

        public void AddOrUpdateSyncTime(string repository, DateTimeOffset syncTime)
        {
            this.RemoveUpdateOrInsert(SyncTimeKey + repository, syncTime);
        }

        public DateTimeOffset GetSyncTime(string repository)
        {
            var obj = TryGetById(SyncTimeKey + repository);
            if (obj == null)
            {
                return DateTimeOffset.MinValue;
            }

            return new DateTimeOffset((DateTime)obj.Value);
        }

        public void AddOrUpdatePauseInfo(PauseReason pauseReason, PauseInfoCollection pauseInfo)
        {
            this.RemoveUpdateOrInsert(PauseTimeKey + pauseReason, pauseInfo);
        }

        public PauseInfoCollection GetPauseInfo(PauseReason pauseReason)
        {
            var obj = TryGetById(PauseTimeKey + pauseReason);
            if (obj == null)
            {
                return new PauseInfoCollection();
            }

            return new PauseInfoCollection(((ICollection<object>)obj.Value).Select(innerObj =>
            {
                var dictionary = (IDictionary<string, object>)innerObj;
                return new PauseInfo((DateTime)dictionary[nameof(PauseInfo.StartTime)], (DateTime?)dictionary[nameof(PauseInfo.EndTime)]);
            }));
        }
    }
}