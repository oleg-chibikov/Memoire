using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Yandex.JsonConverters;

namespace Remembrance.Translate.Yandex.ContractResolvers
{
    internal class TranslationResultContractResolver : CustomContractResolver
    {
        protected override Dictionary<string, string> PropertyMappings { get; } =
            new Dictionary<string, string>
            {
                { nameof(TranslationResult.PartOfSpeechTranslations), "def" },
                { nameof(PartOfSpeechTranslation.Transcription), "ts" },
                { nameof(PartOfSpeechTranslation.TranslationVariants), "tr" },
                { nameof(TranslationVariant.Synonyms), "syn" },
                { nameof(TranslationVariant.Examples), "ex" },
                { nameof(TranslationVariant.Meanings), "mean" },
                { nameof(Example.Translations), "tr" },
                { nameof(Word.NounAnimacy), "anm" },
                { nameof(Word.NounGender), "gen" },
                { nameof(Word.PartOfSpeech), "pos" },
                { nameof(Word.Text), "text" },
                { nameof(Word.VerbType), "asp" }
            };

        protected override Dictionary<Type, JsonConverter> PropertyConverters { get; } = new Dictionary<Type, JsonConverter>
        {
            { typeof(PartOfSpeech), new PartOfSpeechConverter() }
        };
    }
}