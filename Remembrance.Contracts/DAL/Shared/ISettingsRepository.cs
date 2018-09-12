using System;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.Shared
{
    public interface ISettingsRepository : IRepository<Settings, string>, ISharedRepository, IDisposable
    {
        TimeSpan CardShowFrequency { get; set; }

        string PreferredLanguage { get; set; }

        Speaker TtsSpeaker { get; set; }

        VoiceEmotion TtsVoiceEmotion { get; set; }
    }
}