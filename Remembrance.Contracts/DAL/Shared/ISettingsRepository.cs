using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ISettingsRepository : ITrackedRepository<Settings, string>
    {
        TimeSpan CardShowFrequency { get; set; }

        [CanBeNull]
        ICollection<ProcessInfo> BlacklistedProcesses { get; set; }

        Speaker TtsSpeaker { get; set; }

        VoiceEmotion TtsVoiceEmotion { get; set; }
    }
}