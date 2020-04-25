using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Contracts.ImageSearch.Data.Qwant;
using Remembrance.Core.ImageSearch.Qwant.ContractResolvers;
using Remembrance.Resources;
using Scar.Common;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch.Qwant
{
    sealed class ImageSearcher : IImageSearcher, IDisposable
    {
        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { ContractResolver = new ImageSearchResultContractResolver() };

        readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.qwant.com/api/search/"),
            DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36" } }
        };

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        readonly IRateLimiter _rateLimiter;

        readonly ISharedSettingsRepository _sharedSettingsRepository;

        public ImageSearcher(ILogger<ImageSearcher> logger, IMessageHub messageHub, IRateLimiter rateLimiter, ISharedSettingsRepository sharedSettingsRepository)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyCollection<ImageInfo>?> SearchImagesAsync(string text, CancellationToken cancellationToken, int skip, int count, string? language)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            language = language != null ? $"&locale={language}_{language}" : null;
            var uriPart = $"images?count={count}&offset={skip}&q={text}&t=images{language}&uiv=4";
            _logger.LogTrace("Searching images: {0}...", _httpClient.BaseAddress + uriPart);
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode != 429)
                    {
                        throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                    }

                    if (_sharedSettingsRepository.SolveQwantCaptcha)
                    {
                        _rateLimiter.Throttle(
                            TimeSpan.FromSeconds(10),
                            () =>
                            {
                                _logger.LogTrace("Opening browser at Qwant.com to solve the captcha...");
                                Process.Start($"https://www.qwant.com/?q={text}&t=images");
                                _messageHub.Publish(Texts.BrowserWasOpened.ToWarning());
                            },
                            skipLast: true);
                    }

                    _logger.LogWarning("Cannot search images for {0}. Response StatusCode is {1} and ReasonPhrase {2}", text, response.StatusCode, response.ReasonPhrase);
                    return null;
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserialized = JsonConvert.DeserializeObject<QwantResponse>(result, SerializerSettings);
                return deserialized?.Data.Result.Items;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException || ex is JsonException)
            {
                _messageHub.Publish(Errors.CannotGetQwantResults.ToError(ex));
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
