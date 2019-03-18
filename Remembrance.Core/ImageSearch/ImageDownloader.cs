using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch
{
    [UsedImplicitly]
    internal class ImageDownloader : IImageDownloader
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        public ImageDownloader([NotNull] ILog logger, [NotNull] IMessageHub messageHub)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
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
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotDownloadImage.ToError(ex));
                return null;
            }
        }
    }
}