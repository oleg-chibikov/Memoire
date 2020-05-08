using System.Collections.Generic;

namespace Remembrance.Contracts.Translate.Data.Predictor
{
    public sealed class PredictionResult
    {
        public bool EndOfWord { get; set; }

        public int Position { get; set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<string> PredictionVariants { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
