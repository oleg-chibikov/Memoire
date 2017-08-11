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
            if (word == null)
                throw new ArgumentNullException(nameof(word));

            Text = word.Text;
            PartOfSpeech = word.PartOfSpeech;
        }

        [UsedImplicitly]
        public ExchangeWord()
        {
        }

        [JsonProperty("Text", Required = Required.Always)]
        [NotNull]
        public string Text
        {
            get;
            [UsedImplicitly]
            set;
        }

        [JsonProperty("PartOfSpeech", Required = Required.Always)]
        public PartOfSpeech PartOfSpeech
        {
            get;
            [UsedImplicitly]
            set;
        }
    }
}