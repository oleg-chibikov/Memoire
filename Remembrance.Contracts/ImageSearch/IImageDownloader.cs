using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageDownloader
    {
        [NotNull]
        [ItemCanBeNull]
        Task<byte[]> DownloadImageAsync([NotNull] string imageUrl);
    }
}