using System.Collections.Generic;

namespace Remembrance.Contracts.ImageSearch.Data.Qwant
{
    public sealed class QwantResult
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<ImageInfo> Items { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}