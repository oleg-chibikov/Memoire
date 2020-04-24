using System.Collections.Generic;

namespace Remembrance.Contracts.Classification.Data.UClassify
{
    public class UClassifyTopicsResponseItem
    {
        public decimal TextCoverage { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<UClassifyTopicsClassification> Items { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
