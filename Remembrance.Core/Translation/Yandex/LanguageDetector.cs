using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Newtonsoft.Json;
using Remembrance.Contracts;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.LanguageDetector;
using Remembrance.Core.Translation.Yandex.ContractResolvers;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Translation.Yandex
{
    sealed class LanguageDetector : ILanguageDetector
    {
        public const string BaseAddress = "https://translate.yandex.net/api/v1.5/tr.json/";
        static readonly JsonSerializerSettings ListResultSettings = new JsonSerializerSettings { ContractResolver = new ListResultContractResolver() };

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = new DetectionResultContractResolver() };

        readonly HttpClient _httpClient;

        readonly IMessageHub _messageHub;

        public LanguageDetector(IMessageHub messageHub, HttpClient httpClient)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<DetectionResult> DetectLanguageAsync(string text, CancellationToken cancellationToken)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            try
            {
                var uriPart = $"detect?key={YandexConstants.ApiKey}&text={text}&options=1";
                var currentCulture = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
                if (currentCulture != Constants.EnLanguageTwoLetters)
                {
                    uriPart += $"&hint={currentCulture}";
                }

                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<DetectionResult>(result, SerializerSettings) ?? throw new InvalidOperationException("Cannot deserialize DetectionResult");
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is JsonException)
            {
                _messageHub.Publish(Errors.CannotDetectLanguage.ToError(ex));
                return new DetectionResult { Code = Constants.EnLanguageTwoLetters, Language = Constants.EnLanguage };
            }
        }

        public async Task<LanguageListResult?> ListLanguagesAsync(string ui, CancellationToken cancellationToken)
        {
            _ = ui ?? throw new ArgumentNullException(nameof(ui));
            var uriPart = $"getLangs?key={YandexConstants.ApiKey}&ui={ui}";
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<LanguageListResult>(result, ListResultSettings);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is JsonException)
            {
                _messageHub.Publish(Errors.CannotListLanguages.ToError(ex));
                return null;
            }
        }
    }
}
