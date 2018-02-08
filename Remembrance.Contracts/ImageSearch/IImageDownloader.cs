using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageDownloader
    {
        [ItemCanBeNull]
        Task<byte[]> DownloadImageAsync([NotNull] string imageUrl, CancellationToken cancellationToken);

        [NotNull]
        BitmapImage LoadImage([NotNull] byte[] imageBytes);
    }
}