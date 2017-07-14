using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.DAL.Contracts.Model;

namespace Remembrance.Card.Management.Data
{
    [UsedImplicitly]
    internal class RemembranceExchangeEntry : IExchangeEntry
    {
        public RemembranceExchangeEntry([CanBeNull] HashSet<string> priorityTranslations, [NotNull] TranslationEntry translationEntry)
        {
            PriorityTranslations = priorityTranslations;
            TranslationEntry = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
        }

        [CanBeNull]
        [JsonProperty("PriorityTranslations", Required = Required.AllowNull)]
        public HashSet<string> PriorityTranslations { get; }

        [NotNull]
        [JsonProperty("TranslationEntry", Required = Required.Always)]
        public TranslationEntry TranslationEntry { get; }

        public string Text => TranslationEntry.Key.Text;
    }
}