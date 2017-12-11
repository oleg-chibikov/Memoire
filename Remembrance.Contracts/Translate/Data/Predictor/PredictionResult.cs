namespace Remembrance.Contracts.Translate.Data.Predictor
{
    public class PredictionResult
    {
        public bool EndOfWord { get; set; }

        public int Position { get; set; }

        public string[] PredictionVariants { get; set; }
    }
}