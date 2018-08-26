using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.WithMembers)]
    public sealed class TranslationResult
    {
        [NotNull]
        public ICollection<PartOfSpeechTranslation> PartOfSpeechTranslations { get; set; }

        [NotNull]
        public ICollection<Word> GetDefaultWords()
        {
            return PartOfSpeechTranslations.Select(x => x.TranslationVariants.First()).Cast<Word>().ToList();
        }
    }
}