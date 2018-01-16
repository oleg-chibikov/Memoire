using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Core.Translation.Yandex.ContractResolvers;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class WordsTranslator : IWordsTranslator
    {
        [NotNull]
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new TranslationResultContractResolver()
        };

        /// <summary>
        /// See https://tech.yandex.ru/dictionary/doc/dg/reference/lookup-docpage/
        /// </summary>
        [NotNull]
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://dictionary.yandex.net/dicservice.json/")
        };

        public async Task<TranslationResult> GetTranslationAsync(string from, string to, string text, string ui, CancellationToken cancellationToken)
        {
            if (from == null)
                throw new ArgumentNullException(nameof(from));
            if (to == null)
                throw new ArgumentNullException(nameof(to));
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));

            // falgs morpho(4) //|family(1)
            var uriPart = $"lookup?srv=tr-text&text={text}&type=&lang={from}-{to}&flags=4&ui={ui}";
            var response = await _httpClient.GetAsync(uriPart, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new TranslationResult
                {
                    PartOfSpeechTranslations = new PartOfSpeechTranslation[0]
                };

            var result = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<TranslationResult>(result, SerializerSettings);
        }

        /*
        private readonly HttpClient translateClient = new HttpClient
        {
            BaseAddress = new Uri("https://translate.yandex.net/api/v1.5/tr.json/")
        };
        
        public async Task<TranslationResult> GetTranslationAsync(string from, string to, string text, string ui)
        {
            var dictionaryTask = GetDictionaryResultAsync(from, to, text, ui);
            var translateTask = GetTranslateResultAsync(from, to, text);
            await Task.WhenAll(dictionaryTask, translateTask).ConfigureAwait(false);
            var dictionaryResult = dictionaryTask.Result;
            var translateResult = translateTask.Result;
            if (string.IsNullOrWhiteSpace(translateResult) || translateResult == text)
                translateResult = dictionaryResult?.PartOfSpeechTranslations.FirstOrDefault()?.TranslationVariants.FirstOrDefault()?.Text;
            if (translateResult == null)
                return null;
            if (dictionaryResult == null)
                dictionaryResult = new TranslationResult { PartOfSpeechTranslations = new PartOfSpeechTranslation[0] };
            dictionaryResult.Text = translateResult;
            return dictionaryResult;
        }

        [NotNull, ItemCanBeNull]
        private async Task<TranslationResult> GetDictionaryResultAsync(string from, string to, string text, string ui)
        {
            var uriPart = $"lookup?srv=tr-text&text={text}&type=&lang={from}-{to}&flags=4&ui={ui}";
            var response = await dictionaryClient.GetAsync(uriPart).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<TranslationResult>(result, TranslationResultSettings);
        }
        private class TextEntriesContainer
        {
            [NotNull, UsedImplicitly, JsonProperty("text")]
            // ReSharper disable once NotNullMemberIsNotInitialized
            public string[] Texts { get; set; }
        }

        [NotNull, ItemCanBeNull]
        private async Task<string> GetTranslateResultAsync(string from, string to, string text)
        {
            var uriPart = $"translate?key={YandexConstants.ApiKey}&lang={from}-{to}&text={text}";
            var response = await translateClient.GetAsync(uriPart).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<TextEntriesContainer>(result, TranslationResultSettings).Texts.First();
        }
        */
    }
}