using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Contracts.ImageSearch.Data;
using Remembrance.Core.ImageSearch.Qwant.ContractResolvers;
using Remembrance.Core.ImageSearch.Qwant.Data;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch.Qwant
{
    [UsedImplicitly]
    internal sealed class ImageSearcher : IImageSearcher
    {
        [NotNull]
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new ImageSearchResultContractResolver()
        };

        [NotNull]
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.qwant.com/api/search/"),
            DefaultRequestHeaders =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36" }
            }
        };

        private readonly object _locker = new object();

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        private bool _captchaPassing;

        public ImageSearcher([NotNull] ILog logger, [NotNull] IMessageHub messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ImageInfo[]> SearchImagesAsync(string text, CancellationToken cancellationToken, int skip, int count, string language)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            language = language != null
                ? $"&lang={language}_{language}"
                : null;
            var uriPart = $"images?count={count}&offset={skip}&q={text}{language}";
            _logger.TraceFormat("Searching images: {0}...", _httpClient.BaseAddress + uriPart);
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    if ((int)response.StatusCode == 429)
                    {
                        if (!_captchaPassing)
                        {
                            lock (_locker)
                            {
                                if (!_captchaPassing)
                                {
                                    _logger.InfoFormat("Opening browser at Qwant.com to solve the captcha...");
                                    Process.Start($"https://www.qwant.com/?q={text}&t=images");
                                    _messenger.Publish(Texts.BrowserWasOpened.ToWarning());
                                    _captchaPassing = true;
                                }
                            }
                        }

                        return null;
                    }

                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                if (_captchaPassing)
                {
                    lock (_locker)
                    {
                        _captchaPassing = false;
                    }
                }

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserialized = JsonConvert.DeserializeObject<QwantResponse>(result, SerializerSettings);
                return deserialized.Data.Result.Items;
            }
            catch (Exception ex)
            {
                _messenger.Publish(Errors.CannotGetQwantResults.ToError(ex));
                return null;
            }
        }
    }
}