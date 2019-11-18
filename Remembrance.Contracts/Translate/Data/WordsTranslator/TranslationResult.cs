using System.Collections.Generic;
using System.Linq;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationResult
    {
        public IReadOnlyCollection<PartOfSpeechTranslation> PartOfSpeechTranslations { get; set; }

        public IEnumerable<Word> GetDefaultWords()
        {
            return PartOfSpeechTranslations.Select(x => x.TranslationVariants.First()).Cast<Word>().ToList();
        }
    }
}