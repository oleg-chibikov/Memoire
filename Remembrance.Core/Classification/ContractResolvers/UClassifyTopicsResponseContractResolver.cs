using System.Collections.Generic;
using Remembrance.Contracts.Classification.Data.UClassify;

namespace Remembrance.Core.Classification.ContractResolvers
{
    sealed class UClassifyTopicsResponseContractResolver : CustomContractResolver
    {
        protected override IReadOnlyDictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(UClassifyTopicsResponseItem.Items), "classification" },
            { nameof(UClassifyTopicsResponseItem.TextCoverage), "textCoverage" },
            { nameof(UClassifyTopicsClassification.Match), "p" },
            { nameof(UClassifyTopicsClassification.ClassName), "className" }
        };
    }
}
