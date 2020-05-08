using System.Collections.Generic;
using System.Linq;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    static class TranslationVariantExtensions
    {
        public static IEnumerable<Word> GetTranslationVariantAndSynonyms(this TranslationVariant translationVariant)
        {
            return translationVariant.Synonyms?.Count > 0
                ? translationVariant.Synonyms.Concat(
                    new[]
                    {
                        translationVariant
                    })
                : new[]
                {
                    translationVariant
                };
        }
    }
}
