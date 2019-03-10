using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class LanguageListResult
    {
        [NotNull]
        public IReadOnlyCollection<string> Directions { get; set; } = new string[0];

        [NotNull]
        public IReadOnlyDictionary<string, string> Languages { get; set; } = new Dictionary<string, string>();
    }
}