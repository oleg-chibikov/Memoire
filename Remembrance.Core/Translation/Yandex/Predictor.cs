using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.Predictor;
using Remembrance.Core.Translation.Yandex.ContractResolvers;
using Remembrance.Resources;
using Scar.Common.Messages;

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

        [NotNull]
        private readonly IMessageHub _messageHub;

        public Predictor([NotNull] ILanguageDetector languageDetector, [NotNull] IMessageHub messageHub)
        {
            _languageDetector = languageDetector ?? throw new ArgumentNullException(nameof(languageDetector));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task<PredictionResult> PredictAsync(string text, int limit, CancellationToken cancellationToken)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var lang = await _languageDetector.DetectLanguageAsync(text, cancellationToken).ConfigureAwait(false);

            var uriPart = $"complete?key={YandexConstants.PredictorApiKey}&q={text}&lang={lang.Language}&limit={limit}";
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<PredictionResult>(result, SerializerSettings);
            }
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotPredict.ToError(ex));
                return null;
            }
        }
    }
}