using System.Threading;
using System.Threading.Tasks;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageDownloader
    {
        Task<byte[]?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken);
    }
}