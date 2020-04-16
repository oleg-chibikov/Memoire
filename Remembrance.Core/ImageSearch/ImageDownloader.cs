using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch
{
    sealed class ImageDownloader : IImageDownloader, IDisposable
    {
        readonly HttpClient _httpClient = new HttpClient();

        readonly ILog _logger;

        readonly IMessageHub _messageHub;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5359:Do Not Disable Certificate Validation", Justification = "No need for certificate validation")]
        public ImageDownloader(ILog logger, IMessageHub messageHub)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<IReadOnlyCollection<byte>?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
        {
            try
            {
                _logger.TraceFormat("Loading image {0}...", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                _messageHub.Publish(Errors.CannotDownloadImage.ToError(ex));
                return null;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
