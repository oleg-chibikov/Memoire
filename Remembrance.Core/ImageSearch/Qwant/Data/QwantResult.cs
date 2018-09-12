using System.Collections.Generic;
using Remembrance.Contracts.ImageSearch.Data;

namespace Remembrance.Core.ImageSearch.Qwant.Data
{
    internal sealed class QwantResult
    {
        public IReadOnlyCollection<ImageInfo> Items { get; set; }
    }
}