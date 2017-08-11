using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.LanguageDetector;
using Remembrance.Core.Translation.Yandex.ContractResolvers;
using Scar.Common.WPF.Localization;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class LanguageDetector : ILanguageDetector
    {
        [NotNull]
        private static readonly JsonSerializerSettings DetectionResultSettings = new JsonSerializerSettings
        {
            ContractResolver = new DetectionResultContractResolver()
        };

        [NotNull]
        private static readonly JsonSerializerSettings ListResultSettings = new JsonSerializerSettings
        {
            ContractResolver = new ListResultContractResolver()
        };

        private readonly ConcurrentDictionary<string, Task<ListResult>> _cacheTasks = new ConcurrentDictionary<string, Task<ListResult>>();

        [NotNull]
        private readonly HttpClient _client = new HttpClient
        {
            BaseAddress = new Uri("https://translate.yandex.net/api/v1.5/tr.json/")
        };

        public async Task<DetectionResult> DetectLanguageAsync(string text, CancellationToken cancellationToken)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var uriPart = $"detect?key={YandexConstants.ApiKey}&text={text}&hint=en,{CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName}";
            var response = await _client.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return new DetectionResult();

            var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<DetectionResult>(result, DetectionResultSettings);
        }

        public async Task<ListResult> ListLanguagesAsync(string ui, CancellationToken cancellationToken)
        {
            if (ui == null)
                throw new ArgumentNullException(nameof(ui));

            return await _cacheTasks.GetOrAdd(
                    ui,
                    async x =>
                    {
                        var uriPart = $"getLangs?key={YandexConstants.ApiKey}&ui={ui}";
                        var response = await _client.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            return new ListResult();

                        var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<ListResult>(result, ListResultSettings);
                    })
                .ConfigureAwait(false);
        }
    }
}