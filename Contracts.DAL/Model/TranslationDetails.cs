using System;
using Scar.Common.DAL.Contracts.Model;
using Scar.Services.Contracts.Data.ExtendedTranslation;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity<TranslationEntryKey>
    {
        public TranslationDetails()
        {
        }

        public TranslationDetails(TranslationResult translationResult, ExtendedTranslationResult? extendedTranslationResult, TranslationEntryKey translationEntryKey)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
            ExtendedTranslationResult = extendedTranslationResult;
        }

        public TranslationResult TranslationResult { get; set; }

        public ExtendedTranslationResult? ExtendedTranslationResult { get; set; }

        public override string ToString()
        {
            return $"Translation details for {Id}";
        }
    }
}
