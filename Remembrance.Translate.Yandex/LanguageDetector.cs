using System;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Translate.Contracts.Data.LanguageDetector;
using Remembrance.Translate.Contracts.Interfaces;
using Remembrance.Translate.Yandex.ContractResolvers;
using Scar.Common.WPF.Localization;

namespace Remembrance.Translate.Yandex
{
    [UsedImplicitly]
    internal sealed class LanguageDetector : ILanguageDetector
    {
        [NotNull]
        private static readonly JsonSerializerSettings DetectionResultSettings = new JsonSerializerSettings { ContractResolver = new DetectionResultContractResolver() };

        [NotNull]
        private static readonly JsonSerializerSettings ListResultSettings = new JsonSerializerSettings { ContractResolver = new ListResultContractResolver() };

        [NotNull]
        private readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://translate.yandex.net/api/v1.5/tr.json/")
        };

        public async Task<DetectionResult> DetectLanguageAsync(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            var uriPart = $"detect?key={YandexConstants.ApiKey}&text={text}&hint=en,{CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName}";
            var response = await client.GetAsync(uriPart).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new DetectionResult();
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DetectionResult>(result, DetectionResultSettings);
        }

        public async Task<ListResult> ListLanguagesAsync(string ui)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));
            var uriPart = $"getLangs?key={YandexConstants.ApiKey}&ui={ui}";
            var response = await client.GetAsync(uriPart).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new ListResult();
            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<ListResult>(result, ListResultSettings);
        }
    }
}