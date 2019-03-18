using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageDownloader
    {
        [ItemCanBeNull]
        [NotNull]
        Task<byte[]?> DownloadImageAsync([NotNull] string imageUrl, CancellationToken cancellationToken);
    }
}