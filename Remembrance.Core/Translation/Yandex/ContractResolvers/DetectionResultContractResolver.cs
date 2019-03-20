using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Core.Translation.Yandex.ContractResolvers
{
    internal sealed class DetectionResultContractResolver : CustomContractResolver
    {
        protected override IReadOnlyDictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(DetectionResult.Code), "code" },
            { nameof(DetectionResult.Language), "lang" }
        };
    }
}