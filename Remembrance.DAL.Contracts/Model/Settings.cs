using System;
using JetBrains.Annotations;
using Remembrance.Translate.Contracts.Data.TextToSpeechPlayer;
using Scar.Common.DAL.Model;
using Scar.Common.WPF.Localization;

namespace Remembrance.DAL.Contracts.Model
{
    public sealed class Settings : Entity<int>
    {
        public Settings()
        {
            CardShowFrequency = TimeSpan.FromMinutes(1);
            TtsSpeaker = Speaker.Alyss;
            TtsVoiceEmotion = VoiceEmotion.Neutral;
            UiLanguage = CultureUtilities.GetCurrentCulture().ToString();
            IsActive = true;
        }

        [CanBeNull]
        public string LastUsedTargetLanguage { get; set; }

        [CanBeNull]
        public string LastUsedSourceLanguage { get; set; }

        public TimeSpan CardShowFrequency { get; set; }

        public Speaker TtsSpeaker { get; set; }

        public string UiLanguage { get; set; }

        public VoiceEmotion TtsVoiceEmotion { get; set; }

        public bool ReverseTranslation { get; set; }

        public bool RandomTranslation { get; set; }

        public bool IsActive { get; set; }
    }
}