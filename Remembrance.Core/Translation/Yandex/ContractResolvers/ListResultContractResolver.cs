using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Core.Translation.Yandex.ContractResolvers
{
    internal sealed class ListResultContractResolver : CustomContractResolver
    {
        protected override Dictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(LanguageListResult.Directions), "dirs" },
            { nameof(LanguageListResult.Languages), "langs" }
        };
    }
}