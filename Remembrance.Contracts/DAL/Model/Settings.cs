using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL.Model;
using Scar.Common.WPF.Localization;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class Settings : TrackedEntity<int>
    {
        public Settings()
        {
            CardShowFrequency = TimeSpan.FromMinutes(1);
            TranslationCloseTimeout = TimeSpan.FromSeconds(10);
            AssessmentSuccessCloseTimeout = TimeSpan.FromSeconds(2);
            AssessmentFailureCloseTimeout = TimeSpan.FromSeconds(5);
            TtsSpeaker = Speaker.Alyss;
            TtsVoiceEmotion = VoiceEmotion.Neutral;
            UiLanguage = CultureUtilities.GetCurrentCulture()
                .ToString();
            IsActive = true;
        }

        [CanBeNull]
        public string LastUsedTargetLanguage { get; set; }

        [CanBeNull]
        public string LastUsedSourceLanguage { get; set; }

        public TimeSpan CardShowFrequency { get; set; }

        public DateTime? LastCardShowTime { get; set; }

        public TimeSpan PausedTime { get; set; }

        public TimeSpan TranslationCloseTimeout { get; set; }

        public TimeSpan AssessmentSuccessCloseTimeout { get; set; }

        public TimeSpan AssessmentFailureCloseTimeout { get; set; }

        public Speaker TtsSpeaker { get; set; }

        public string UiLanguage { get; set; }

        public VoiceEmotion TtsVoiceEmotion { get; set; }

        public bool ReverseTranslation { get; set; }

        public bool RandomTranslation { get; set; }

        public bool IsActive { get; set; }

        public override string ToString()
        {
            return "Settings";
        }
    }
}