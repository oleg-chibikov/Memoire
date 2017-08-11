using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.Model;

namespace Remembrance.Core.CardManagement.Data
{
    [UsedImplicitly]
    internal sealed class RemembranceExchangeEntry : IExchangeEntry
    {
        public RemembranceExchangeEntry([CanBeNull] HashSet<ExchangeWord> priorityTranslations, [NotNull] TranslationEntry translationEntry)
        {
            PriorityTranslations = priorityTranslations;
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
        }

        [CanBeNull]
        [JsonProperty("PriorityTranslations", Required = Required.Default)]
        public HashSet<ExchangeWord> PriorityTranslations { get; }

        [NotNull]
        [JsonProperty("TranslationEntry", Required = Required.Always)]
        public TranslationEntry TranslationEntry { get; }

        [JsonIgnore]
        public string Text => TranslationEntry.Key.Text;
    }
}