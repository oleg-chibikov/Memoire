using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch.Data;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageSearcher
    {
        [NotNull]
        [ItemCanBeNull]
        Task<ImageInfo[]> SearchImagesAsync([NotNull] string text, [NotNull] string language, CancellationToken cancellationToken, int skip = 0, int count = 1);
    }
}