using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.ImageSearch
{
    internal class ImageDownloader : IImageDownloader
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        public ImageDownloader([NotNull] ILog logger, [NotNull] IMessageHub messenger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
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
                _messenger.Publish(Errors.CannotDownloadImage.ToError(ex));
                return null;
            }
        }

        public BitmapImage LoadImage(byte[] imageBytes)
        {
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
    }
}