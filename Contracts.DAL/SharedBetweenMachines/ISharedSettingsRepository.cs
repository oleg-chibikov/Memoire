using System;
using Mémoire.Contracts.DAL.Model;
using Scar.Common.DAL.Contracts;
using Scar.Services.Contracts.Data.TextToSpeech;

namespace Mémoire.Contracts.DAL.SharedBetweenMachines
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

        double ClassificationMinimalThreshold { get; set; }

        ApiKeys ApiKeys { get; set; }

        CardProbabilitySettings CardProbabilitySettings { get; set; }
    }
}
