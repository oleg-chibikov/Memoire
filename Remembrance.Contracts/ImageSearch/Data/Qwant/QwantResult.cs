using System.Collections.Generic;

namespace Remembrance.Contracts.ImageSearch.Data.Qwant
{
    public sealed class QwantResult
    {
        public IReadOnlyCollection<ImageInfo> Items { get; set; }
    }
}