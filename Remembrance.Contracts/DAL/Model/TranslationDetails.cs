using System;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity<int>
    {
        [UsedImplicitly]
        public TranslationDetails()
        {
        }

        public TranslationDetails([NotNull] TranslationResult translationResult, [NotNull] object translationEntryId, [CanBeNull] PrepositionsCollection prepositionsCollection = null)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
            PrepositionsCollection = prepositionsCollection;
        }

        [NotNull]
        public object TranslationEntryId
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public TranslationResult TranslationResult
        {
            get;
            [UsedImplicitly]
            set;
        }

        [CanBeNull]
        public PrepositionsCollection PrepositionsCollection
        {
            get;
            [UsedImplicitly]
            set;
        }

        public override string ToString()
        {
            return $"Translation details for {TranslationEntryId}";
        }
    }
}