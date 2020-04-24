using System.Collections.Generic;

namespace Remembrance.Contracts.Classification.Data.UClassify
{
    public class UClassifyTopicsResponse
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<UClassifyTopicsResponseItem> Items { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
