using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch;

namespace Remembrance.Core.ImageSearch
{
    internal class ImageDownloader : IImageDownloader
    {
        [NotNull]
        private readonly ILog _logger;

        public ImageDownloader([NotNull] ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        public async Task<byte[]> DownloadImageAsync(string imageUrl)
        {
            var httpClient = new HttpClient();
            try
            {
                _logger.TraceFormat("Loading image {0}", imageUrl);
                return await httpClient.GetByteArrayAsync(imageUrl);
            }
            catch (Exception ex)
            {
                _logger.Error("Cannot download image", ex);
                return null;
            }
        }
    }
}