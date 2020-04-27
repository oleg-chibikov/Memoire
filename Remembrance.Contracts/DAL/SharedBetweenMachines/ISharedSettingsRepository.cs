using System;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL;

namespace Remembrance.Contracts.DAL.SharedBetweenMachines
{
    public interface ISharedSettingsRepository : IRepository<ApplicationSettings, string>, ISharedRepository, IDisposable
    {
        TimeSpan CardShowFrequency { get; set; }

        string PreferredLanguage { get; set; }

        Speaker TtsSpeaker { get; set; }

        bool SolveQwantCaptcha { get; set; }

        bool MuteSounds { get; set; }

        VoiceEmotion TtsVoiceEmotion { get; set; }

        int CardsToShowAtOnce { get; set; }
    }
}
