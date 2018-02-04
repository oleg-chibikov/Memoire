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
        public RemembranceExchangeEntry([NotNull] TranslationEntry translationEntry)
        {
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
        }

        [NotNull]
        [JsonProperty("TranslationEntry", Required = Required.Always)]
        public TranslationEntry TranslationEntry { get; }

        [JsonIgnore]
        public string Text => TranslationEntry.Id.Text;

        public override string ToString()
        {
            return TranslationEntry
                   + (TranslationEntry.ManualTranslations != null
                       ? $" [{string.Join(", ", TranslationEntry.ManualTranslations.Select(x => x.Text))}]"
                       : null);
        }
    }
}