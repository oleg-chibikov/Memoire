using System;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;

namespace Remembrance.DAL.SharedBetweenMachines
{
    sealed class SharedSettingsRepository : BaseSettingsRepository, ISharedSettingsRepository
    {
        public SharedSettingsRepository(IRemembrancePathsProvider remembrancePathsProvider, string? directoryPath = null, bool shrink = true) : base(
            directoryPath ?? remembrancePathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(remembrancePathsProvider)),
            nameof(ApplicationSettings),
            shrink)
        {
        }

        public TimeSpan CardShowFrequency
        {
            get => TryGetValue(nameof(CardShowFrequency), TimeSpan.FromMinutes(1));
            set => RemoveUpdateOrInsert(nameof(CardShowFrequency), value);
        }

        public string PreferredLanguage
        {
            get => TryGetValue(nameof(PreferredLanguage), Constants.EnLanguageTwoLetters);
            set => RemoveUpdateOrInsert(nameof(PreferredLanguage), value);
        }

        public Speaker TtsSpeaker
        {
            get => TryGetValue<Speaker>(nameof(TtsSpeaker));
            set => RemoveUpdateOrInsert(nameof(TtsSpeaker), value);
        }

        public bool SolveQwantCaptcha
        {
            get => TryGetValue<bool>(nameof(SolveQwantCaptcha));
            set => RemoveUpdateOrInsert(nameof(SolveQwantCaptcha), value);
        }

        public VoiceEmotion TtsVoiceEmotion
        {
            get => TryGetValue<VoiceEmotion>(nameof(TtsVoiceEmotion));
            set => RemoveUpdateOrInsert(nameof(TtsVoiceEmotion), value);
        }

        public int CardsToShowAtOnce
        {
            get => TryGetValue(nameof(CardsToShowAtOnce), 1);
            set => RemoveUpdateOrInsert(nameof(CardsToShowAtOnce), value);
        }
    }
}
