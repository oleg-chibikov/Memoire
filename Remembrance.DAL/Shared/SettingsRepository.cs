using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.DAL.Local;
using Remembrance.Resources;
using Scar.Common.DAL.LiteDB;

namespace Remembrance.DAL.Shared
{
    [UsedImplicitly]
    internal sealed class SettingsRepository : TrackedLiteDbRepository<Settings, string>, ISettingsRepository
    {
        public SettingsRepository([CanBeNull] string directoryPath, [CanBeNull] string fileName = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, fileName, shrink)
        {
        }

        public SettingsRepository([CanBeNull] string directoryPath = null, bool shrink = true)
            : base(directoryPath ?? Paths.SharedDataPath, null, shrink)
        {
        }

        public TimeSpan CardShowFrequency
        {
            get
            {
                var obj = TryGetById(nameof(CardShowFrequency));
                return obj == null ? TimeSpan.FromMinutes(1) : TimeSpan.FromTicks((long)obj.Value);
            }

            set => this.RemoveUpdateOrInsert(nameof(CardShowFrequency), value);
        }

        public ICollection<ProcessInfo> BlacklistedProcesses
        {
            get
            {
                var obj = TryGetById(nameof(BlacklistedProcesses));
                return ((ICollection<object>)obj?.Value)?.Select(
                        innerObj =>
                        {
                            var dictionary = (IDictionary<string, object>)innerObj;
                            return new ProcessInfo((string)dictionary[nameof(ProcessInfo.Name)], (string)dictionary[nameof(ProcessInfo.FilePath)]);
                        })
                    .ToArray();
            }

            set => this.RemoveUpdateOrInsert(nameof(BlacklistedProcesses), value);
        }

        public Speaker TtsSpeaker
        {
            get
            {
                var obj = TryGetById(nameof(TtsSpeaker));
                return obj == null ? Speaker.Alyss : (Speaker)Enum.Parse(typeof(Speaker), (string)obj.Value);
            }

            set => this.RemoveUpdateOrInsert(nameof(TtsSpeaker), value);
        }

        public VoiceEmotion TtsVoiceEmotion
        {
            get
            {
                var obj = TryGetById(nameof(TtsVoiceEmotion));
                return obj == null ? VoiceEmotion.Neutral : (VoiceEmotion)Enum.Parse(typeof(VoiceEmotion), (string)obj.Value);
            }

            set => this.RemoveUpdateOrInsert(nameof(TtsVoiceEmotion), value);
        }
    }
}