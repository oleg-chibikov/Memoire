using System.Collections.Generic;
using Remembrance.Translate.Contracts.Data.LanguageDetector;

namespace Remembrance.Translate.Yandex.ContractResolvers
{
    internal class DetectionResultContractResolver : CustomContractResolver
    {
        protected override Dictionary<string, string> PropertyMappings { get; } =
            new Dictionary<string, string>
            {
                { nameof(DetectionResult.Code), "code" },
                { nameof(DetectionResult.Language), "lang" }
            };
    }
}