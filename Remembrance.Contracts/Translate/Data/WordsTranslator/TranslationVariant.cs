using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class TranslationVariant : Word
    {
        [CanBeNull]
        public ICollection<Example> Examples { get; set; }

        [CanBeNull]
        public ICollection<Word> Meanings { get; set; }

        [CanBeNull]
        public ICollection<Word> Synonyms { get; set; }
    }
}