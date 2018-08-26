using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.Predictor
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class PredictionResult
    {
        public bool EndOfWord { get; set; }

        public int Position { get; set; }

        public ICollection<string> PredictionVariants { get; set; }
    }
}