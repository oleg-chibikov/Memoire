using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Core.Translation.Yandex.ContractResolvers;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Translation.Yandex
{
    sealed class WordsTranslator : IWordsTranslator
    {
        public const string BaseAddress = "https://dictionary.yandex.net/dicservice.json/";
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = new TranslationResultContractResolver() };

        /// <summary>
        /// See https://tech.yandex.ru/dictionary/doc/dg/reference/lookup-docpage/.
        /// </summary>
        readonly HttpClient _httpClient;

        readonly IMessageHub _messageHub;

        public WordsTranslator(IMessageHub messageHub, HttpClient httpClient)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<TranslationResult?> GetTranslationAsync(string sourceLanguage, string targetLanguage, string text, string ui, CancellationToken cancellationToken)
        {
            _ = sourceLanguage ?? throw new ArgumentNullException(nameof(sourceLanguage));
            _ = targetLanguage ?? throw new ArgumentNullException(nameof(targetLanguage));
            _ = text ?? throw new ArgumentNullException(nameof(text));
            _ = ui ?? throw new ArgumentNullException(nameof(ui));

            // flags morpho(4) //|family(1)
            var uriPart = $"lookup?srv=tr-text&text={text}&type=&lang={sourceLanguage}-{targetLanguage}&flags=4&ui={ui}";
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<TranslationResult>(result, SerializerSettings);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is JsonException)
            {
                _messageHub.Publish(string.Format(CultureInfo.InvariantCulture, Errors.CannotTranslate, text + $" [{sourceLanguage}->{targetLanguage}]").ToError(ex));
                return null;
            }
        }
    }
}
