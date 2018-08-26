using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.LanguageDetector;
using Remembrance.Core.Translation.Yandex.ContractResolvers;
using Remembrance.Resources;
using Scar.Common.Messages;
using Scar.Common.WPF.Localization;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class LanguageDetector : ILanguageDetector
    {
        [NotNull]
        private static readonly JsonSerializerSettings ListResultSettings = new JsonSerializerSettings
        {
            ContractResolver = new ListResultContractResolver()
        };

        [NotNull]
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DetectionResultContractResolver()
        };

        [NotNull]
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://translate.yandex.net/api/v1.5/tr.json/")
        };

        [NotNull]
        private readonly IMessageHub _messageHub;

        public LanguageDetector([NotNull] IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task<DetectionResult> DetectLanguageAsync(string text, CancellationToken cancellationToken)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            try
            {
                var uriPart = $"detect?key={YandexConstants.ApiKey}&text={text}&options=1";
                var currentCulture = CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName;
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
                return JsonConvert.DeserializeObject<DetectionResult>(result, SerializerSettings);
            }
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotDetectLanguage.ToError(ex));
                return new DetectionResult
                {
                    Code = Constants.EnLanguageTwoLetters,
                    Language = Constants.EnLanguage
                };
            }
        }

        public async Task<LanguageListResult> ListLanguagesAsync(string ui, CancellationToken cancellationToken)
        {
            if (ui == null)
            {
                throw new ArgumentNullException(nameof(ui));
            }

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
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotListLanguages.ToError(ex));
                return null;
            }
        }
    }
}