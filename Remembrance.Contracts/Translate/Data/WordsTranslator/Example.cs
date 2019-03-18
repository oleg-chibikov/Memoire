using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class Example : TextEntry
    {
        [CanBeNull]
        public IReadOnlyCollection<TextEntry>? Translations { get; set; }
    }
}