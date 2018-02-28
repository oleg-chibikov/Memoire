using System;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.CardManagement.Data
{
    [UsedImplicitly]
    internal sealed class RemembranceExchangeEntry : IExchangeEntry
    {
        public RemembranceExchangeEntry([NotNull] TranslationEntry translationEntry, [NotNull] LearningInfo learningInfo)
        {
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            LearningInfo = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
        }

        [NotNull]
        [JsonProperty("LearningInfo", Required = Required.Always)]
        public LearningInfo LearningInfo { get; }

        [JsonIgnore]
        public string Text => TranslationEntry.Id.Text;

        [NotNull]
        [JsonProperty("TranslationEntry", Required = Required.Always)]
        public TranslationEntry TranslationEntry { get; }

        public override string ToString()
        {
            return TranslationEntry + (TranslationEntry.ManualTranslations != null ? $" [{string.Join(", ", TranslationEntry.ManualTranslations.Select(x => x.Text))}]" : null);
        }
    }
}