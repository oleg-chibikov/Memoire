using System.Collections.Generic;
using System.Linq;

// ReSharper disable NotNullMemberIsNotInitialized
namespace Remembrance.Contracts.Translate.Data.WordsTranslator
{
    public sealed class TranslationResult
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IReadOnlyCollection<PartOfSpeechTranslation> PartOfSpeechTranslations { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public IEnumerable<Word> GetDefaultWords()
        {
            return PartOfSpeechTranslations.Select(x => x.TranslationVariants.First()).Cast<Word>().ToList();
        }
    }
}
