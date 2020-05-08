using System;
using System.Linq;
using Mémoire.Contracts.DAL.Model;
using Newtonsoft.Json;

namespace Mémoire.Core.CardManagement.Data
{
    sealed class ExchangeEntry : IExchangeEntry
    {
        public ExchangeEntry(TranslationEntry translationEntry, LearningInfo learningInfo)
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
