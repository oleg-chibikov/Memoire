using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch
{
    sealed class ImageDownloader : IImageDownloader
    {
        readonly HttpClient _httpClient;

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        [SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "No need for certificate validation")]
        public ImageDownloader(ILogger<ImageDownloader> logger, IMessageHub messageHub, HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<IReadOnlyCollection<byte>?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogTrace("Loading image {0}...", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is InvalidOperationException)
            {
                _messageHub.Publish(Errors.CannotDownloadImage.ToError(ex));
                return null;
            }
        }
    }
}
