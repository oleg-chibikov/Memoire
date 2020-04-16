using System.Collections.Generic;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    public sealed class LanguageListResult
    {
        public IReadOnlyCollection<string> Directions { get; set; } = System.Array.Empty<string>();

        public IReadOnlyDictionary<string, string> Languages { get; set; } = new Dictionary<string, string>();
    }
}
