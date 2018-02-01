using System;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Scar.Common.DAL.Model;

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
        }

        public TimeSpan CardShowFrequency { get; set; }

        public TimeSpan TranslationCloseTimeout { get; set; }

        public TimeSpan AssessmentSuccessCloseTimeout { get; set; }

        public TimeSpan AssessmentFailureCloseTimeout { get; set; }

        public Speaker TtsSpeaker { get; set; }

        public VoiceEmotion TtsVoiceEmotion { get; set; }

        public bool ReverseTranslation { get; set; }

        public bool RandomTranslation { get; set; }

        public override string ToString()
        {
            return "Settings";
        }
    }
}