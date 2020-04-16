using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Core.Translation.Yandex.ContractResolvers
{
    sealed class ListResultContractResolver : CustomContractResolver
    {
        protected override IReadOnlyDictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(LanguageListResult.Directions), "dirs" }, { nameof(LanguageListResult.Languages), "langs" }
        };
    }
}
