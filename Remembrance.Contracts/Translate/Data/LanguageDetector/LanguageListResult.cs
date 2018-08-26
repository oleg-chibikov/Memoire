using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.LanguageDetector
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class LanguageListResult
    {
        [NotNull]
        public ICollection<string> Directions { get; set; } = new string[0];

        [NotNull]
        public IDictionary<string, string> Languages { get; set; } = new Dictionary<string, string>();
    }
}