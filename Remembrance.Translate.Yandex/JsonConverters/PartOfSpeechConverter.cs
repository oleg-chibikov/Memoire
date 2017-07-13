using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Translate.Contracts.Data.WordsTranslator;

namespace Remembrance.Translate.Yandex.JsonConverters
{
    internal sealed class PartOfSpeechConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        [NotNull]
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, [NotNull] JsonSerializer serializer)
        {
            var partOfSpeechString = serializer.Deserialize<string>(reader);
            switch (partOfSpeechString.ToLowerInvariant())
            {
                case "noun":
                    return PartOfSpeech.Noun;
                case "verb":
                    return PartOfSpeech.Verb;
                case "adjective":
                    return PartOfSpeech.Adjective;
                case "participle":
                    return PartOfSpeech.Participle;
                case "adverbial participle":
                    return PartOfSpeech.AdverbialParticiple;
                case "conjunction":
                    return PartOfSpeech.Conjunction;
                case "pronoun":
                    return PartOfSpeech.Pronoun;
                case "numeral":
                    return PartOfSpeech.Numeral;
                case "adverb":
                    return PartOfSpeech.Adverb;
                case "preposition":
                    return PartOfSpeech.Preposition;
                case "particle":
                    return PartOfSpeech.Particle;
                default:
                    return PartOfSpeech.Unknown;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Left as an exercise to the reader :)
            throw new NotImplementedException();
        }
    }
}