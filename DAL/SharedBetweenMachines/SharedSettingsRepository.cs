using System;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.TextToSpeech;

namespace Mémoire.DAL.SharedBetweenMachines
{
    sealed class SharedSettingsRepository : BaseSettingsRepository, ISharedSettingsRepository
    {
        public SharedSettingsRepository(IPathsProvider pathsProvider, string? directoryPath = null, bool shrink = true) : base(
            directoryPath ?? pathsProvider?.LocalSharedDataPath ?? throw new ArgumentNullException(nameof(pathsProvider)),
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
            get => TryGetValue(nameof(PreferredLanguage), LanguageConstants.EnLanguageTwoLetters);
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

        public bool MuteSounds
        {
            get => TryGetValue<bool>(nameof(MuteSounds));
            set => RemoveUpdateOrInsert(nameof(MuteSounds), value);
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

        public double ClassificationMinimalThreshold
        {
            get => TryGetValue(nameof(ClassificationMinimalThreshold), 0.11);
            set => RemoveUpdateOrInsert(nameof(ClassificationMinimalThreshold), value);
        }

        public ApiKeys ApiKeys
        {
            get => TryGetValue(nameof(ApiKeys), ApiKeys.CreateDefault());
            set => RemoveUpdateOrInsert(nameof(ApiKeys), value);
        }

        public CardProbabilitySettings CardProbabilitySettings
        {
            get => TryGetValue(nameof(CardProbabilitySettings), CardProbabilitySettings.CreateDefault());
            set => RemoveUpdateOrInsert(nameof(CardProbabilitySettings), value);
        }
    }
}
