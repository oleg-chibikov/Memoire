using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity<TranslationEntryKey>
    {
        [UsedImplicitly]
        public TranslationDetails()
        {
        }

        //TODO: Combine with TranslationResult
        public TranslationDetails([NotNull] TranslationResult translationResult, [NotNull] TranslationEntryKey translationEntryKey)
        {
            Id = translationEntryKey ?? throw new ArgumentNullException(nameof(translationEntryKey));
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
        }

        [NotNull]
        public TranslationResult TranslationResult { get; set; }

        public override string ToString()
        {
            return $"Translation details for {Id}";
        }
    }
}