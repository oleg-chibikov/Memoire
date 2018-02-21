using System.Collections.Generic;
using Remembrance.Contracts.ImageSearch.Data;

namespace Remembrance.Core.ImageSearch.Qwant.Data
{
    internal sealed class QwantResponse
    {
        public QwantData Data { get; set; }
    }

    internal sealed class QwantData
    {
        public QwantResult Result { get; set; }
    }

    internal sealed class QwantResult
    {
        public ICollection<ImageInfo> Items { get; set; }
    }
}