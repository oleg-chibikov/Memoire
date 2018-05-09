using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ISettingsRepository : IRepository<Settings, string>, ISharedRepository, IDisposable
    {
        [CanBeNull]
        ICollection<ProcessInfo> BlacklistedProcesses { get; set; }

        TimeSpan CardShowFrequency { get; set; }

        Speaker TtsSpeaker { get; set; }

        VoiceEmotion TtsVoiceEmotion { get; set; }

        string PreferredLanguage { get; set; }
    }
}