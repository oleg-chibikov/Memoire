using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.ImageSearch.Data;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageSearcher
    {
        [ItemCanBeNull]
        [NotNull]
        Task<ICollection<ImageInfo>> SearchImagesAsync([NotNull] string text, CancellationToken cancellationToken, int skip = 0, int count = 1, [CanBeNull] string language = null);
    }
}