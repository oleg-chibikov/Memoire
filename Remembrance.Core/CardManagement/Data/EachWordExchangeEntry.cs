using System;
using Newtonsoft.Json;

namespace Remembrance.Core.CardManagement.Data
{
    internal sealed class EachWordExchangeEntry : IExchangeEntry
    {
        public EachWordExchangeEntry(string text, string? translation)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Translation = translation;
        }

        [JsonProperty("Translation", Required = Required.Always)]
        public string? Translation { get; }

        [JsonProperty("Word", Required = Required.Always)]
        public string Text { get; }

        public override string ToString()
        {
            return $"{Text} -> {Translation}";
        }
    }
}