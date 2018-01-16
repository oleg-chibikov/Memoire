using System.Collections.Generic;
using Remembrance.Contracts.Translate.Data.Predictor;

namespace Remembrance.Core.Translation.Yandex.ContractResolvers
{
    internal sealed class PredictionResultContractResolver : CustomContractResolver
    {
        protected override Dictionary<string, string> PropertyMappings { get; } = new Dictionary<string, string>
        {
            { nameof(PredictionResult.EndOfWord), "endOfWord" },
            { nameof(PredictionResult.Position), "pos" },
            { nameof(PredictionResult.PredictionVariants), "text" }
        };
    }
}