using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class TranslationVariant : Word
    {
        [CanBeNull]
        public IReadOnlyCollection<Example>? Examples { get; set; }

        [CanBeNull]
        public IReadOnlyCollection<Word>? Meanings { get; set; }

        [CanBeNull]
        public IReadOnlyCollection<Word>? Synonyms { get; set; }
    }
}