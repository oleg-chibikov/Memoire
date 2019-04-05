using System;
using System.Linq;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.CardManagement.Data
{
    internal sealed class RemembranceExchangeEntry : IExchangeEntry
    {
        public RemembranceExchangeEntry(TranslationEntry translationEntry, LearningInfo learningInfo)
        {
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            LearningInfo = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
        }

        [JsonProperty("LearningInfo", Required = Required.Always)]
        public LearningInfo LearningInfo { get; }

        [JsonProperty("TranslationEntry", Required = Required.Always)]
        public TranslationEntry TranslationEntry { get; }

        [JsonIgnore]
        public string Text => TranslationEntry.Id.Text;

        public override string ToString()
        {
            return TranslationEntry + (TranslationEntry.ManualTranslations != null ? $" [{string.Join(", ", TranslationEntry.ManualTranslations.Select(x => x.Text))}]" : null);
        }
    }
}