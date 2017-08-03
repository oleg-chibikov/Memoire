using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    public sealed class DetectionResult
    {
        [CanBeNull]
        [UsedImplicitly]
        public string Code { get; set; }

        [CanBeNull]
        [UsedImplicitly]
        public string Language { get; set; }
    }
}