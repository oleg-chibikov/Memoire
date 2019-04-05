using System.Collections.Generic;

namespace Remembrance.Contracts.Translate.Data.Predictor
{
    public sealed class PredictionResult
    {
        public bool EndOfWord { get; set; }

        public int Position { get; set; }

        public IReadOnlyCollection<string> PredictionVariants { get; set; }
    }
}