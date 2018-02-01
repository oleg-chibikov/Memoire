using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.Core.CardManagement.Data
{
    internal sealed class ExchangeWord : IWord
    {
        public ExchangeWord([NotNull] IWord word)
        {
            WordText = word?.WordText ?? throw new ArgumentNullException(nameof(word));
            PartOfSpeech = word.PartOfSpeech;
        }

        public ExchangeWord()
        {
        }

        [JsonProperty("Text", Required = Required.Always)]
        public string WordText { get; set; }

        [JsonProperty("PartOfSpeech", Required = Required.Always)]
        public PartOfSpeech PartOfSpeech { get; set; }
    }
}