using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.Predictor;
using Remembrance.Core.Translation.Yandex.ContractResolvers;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class Predictor : IPredictor
    {
        [NotNull]
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new PredictionResultContractResolver()
        };

        [NotNull]
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://predictor.yandex.net/api/v1/predict.json/")
        };

        [NotNull]
        private readonly ILanguageDetector _languageDetector;

        public Predictor([NotNull] ILanguageDetector languageDetector)
        {
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
        }

        public async Task<PredictionResult> PredictAsync(string text, int limit, CancellationToken cancellationToken)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var lang = await _languageDetector.DetectLanguageAsync(text, cancellationToken)
                .ConfigureAwait(false);

            var uriPart = $"complete?key={YandexConstants.PredictorApiKey}&q={text}&lang={lang.Language}&limit={limit}";
            var response = await _httpClient.GetAsync(uriPart, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new PredictionResult();

            var result = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            return JsonConvert.DeserializeObject<PredictionResult>(result, SerializerSettings);
        }
    }
}