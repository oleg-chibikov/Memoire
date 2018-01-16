using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    public sealed class ListResult
    {
        [NotNull]
        public string[] Directions { get; set; } = new string[0];

        [NotNull]
        public Dictionary<string, string> Languages { get; set; } = new Dictionary<string, string>();
    }
}