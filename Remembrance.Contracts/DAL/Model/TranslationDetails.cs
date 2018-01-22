using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity
    {
        [UsedImplicitly]
        public TranslationDetails()
        {
        }

        public TranslationDetails([NotNull] TranslationResult translationResult, [NotNull] object translationEntryId)
        {
            Id = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
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