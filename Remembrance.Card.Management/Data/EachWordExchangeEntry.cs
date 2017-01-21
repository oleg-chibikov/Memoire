using System;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Remembrance.Card.Management.Data
{
    [UsedImplicitly]
    internal class EachWordExchangeEntry : IExchangeEntry
    {
        public EachWordExchangeEntry([NotNull] string text, [CanBeNull] string translation)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            Text = text;
            Translation = translation;
        }

        [CanBeNull, JsonProperty("Translation", Required = Required.Always)]
        public string Translation { get; }

        [JsonProperty("Word", Required = Required.Always)]
        public string Text { get; }
    }
}