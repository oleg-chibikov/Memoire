using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Remembrance.Contracts.ImageSearch
{
    public interface IImageDownloader
    {
        [SuppressMessage("Design", "CA1054:Uri parameters should not be strings", Justification = "No need as it is used only with string")]
        Task<IReadOnlyCollection<byte>?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken);
    }
}
