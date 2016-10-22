using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

// ReSharper disable NotNullMemberIsNotInitialized

namespace Remembrance.Translate.Contracts.Data.WordsTranslator
{
    public sealed class TranslationResult
    {
        [NotNull, UsedImplicitly]
        public PartOfSpeechTranslation[] PartOfSpeechTranslations { get; set; }

        public IList<PriorityWord> GetDefaultWords()
        {
            return PartOfSpeechTranslations.Select(x => x.TranslationVariants.First()).Cast<PriorityWord>().ToList();
        }
    }
}