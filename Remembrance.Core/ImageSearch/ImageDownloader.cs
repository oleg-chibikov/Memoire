using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch;

namespace Remembrance.Core.ImageSearch
{
    internal class ImageDownloader : IImageDownloader
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [NotNull]
        private readonly ILog _logger;

        public ImageDownloader([NotNull] ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken)
        {
            try
            {
                _logger.TraceFormat("Loading image {0}", imageUrl);
                var response = await _httpClient.GetAsync(imageUrl, cancellationToken)
                    .ConfigureAwait(false);
                return await response.Content.ReadAsByteArrayAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot download image", ex);
                return null;
            }
        }
    }
}