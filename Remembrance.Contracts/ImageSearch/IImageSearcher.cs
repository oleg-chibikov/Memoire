using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.ImageSearch.Data;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageSearcher
    {
        Task<IReadOnlyCollection<ImageInfo>?> SearchImagesAsync(string text, CancellationToken cancellationToken, int skip = 0, int count = 1, string? language = null);
    }
}