using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Scar.Common.DAL.Model;
using Scar.Common.WPF.Localization;

namespace Remembrance.Contracts.DAL.Model
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class LocalSettings : Entity<int>
    {
        public LocalSettings()
        {
            UiLanguage = CultureUtilities.GetCurrentCulture().ToString();
            IsActive = true;
            SyncTimes = new Dictionary<string, DateTimeOffset>();
        }

        public bool IsActive { get; set; }

        public DateTime? LastCardShowTime { get; set; }

        [CanBeNull]
        public string LastUsedSourceLanguage { get; set; }

        [CanBeNull]
        public string LastUsedTargetLanguage { get; set; }

        public TimeSpan PausedTime { get; set; }

        public IDictionary<string, DateTimeOffset> SyncTimes { get; set; }

        public string UiLanguage { get; set; }

        public override string ToString()
        {
            return "LocalSettings";
        }
    }
}