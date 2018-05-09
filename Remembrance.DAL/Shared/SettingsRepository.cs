using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class SettingsRepository : BaseSettingsRepository, ISettingsRepository
    {
        public SettingsRepository([CanBeNull] string directoryPath = null, bool shrink = true)
            : base(directoryPath ?? RemembrancePaths.LocalSharedDataPath, nameof(Settings), shrink)
        {
        }

        public ICollection<ProcessInfo> BlacklistedProcesses
        {
            get => TryGetValue<ICollection<ProcessInfo>>(nameof(BlacklistedProcesses));
            set => RemoveUpdateOrInsert(nameof(BlacklistedProcesses), value);
        }

        public TimeSpan CardShowFrequency
        {
            get => TryGetValue(nameof(CardShowFrequency), TimeSpan.FromMinutes(1));
            set => RemoveUpdateOrInsert(nameof(CardShowFrequency), value);
        }

        public Speaker TtsSpeaker
        {
            get => TryGetValue<Speaker>(nameof(TtsSpeaker));
            set => RemoveUpdateOrInsert(nameof(TtsSpeaker), value);
        }

        public VoiceEmotion TtsVoiceEmotion
        {
            get => TryGetValue<VoiceEmotion>(nameof(TtsVoiceEmotion));
            set => RemoveUpdateOrInsert(nameof(TtsVoiceEmotion), value);
        }

        public string PreferredLanguage
        {
            get => TryGetValue(nameof(PreferredLanguage), Constants.EnLanguageTwoLetters);
            set => RemoveUpdateOrInsert(nameof(PreferredLanguage), value);
        }
    }
}