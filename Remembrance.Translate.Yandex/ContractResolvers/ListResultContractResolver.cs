using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.LanguageDetector;

namespace Remembrance.Translate.Yandex.ContractResolvers
{
    internal sealed class ListResultContractResolver : CustomContractResolver
    {
        protected override Dictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            {nameof(ListResult.Directions), "dirs"},
            {nameof(ListResult.Languages), "langs"}
        };
    }
}