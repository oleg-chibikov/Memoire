using System;
using System.Linq;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.DAL.Model;

namespace Remembrance.Contracts.DAL.Model
{
    public sealed class TranslationDetails : Entity<int>
    {
        [UsedImplicitly]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TranslationDetails()
        {
        }

        public TranslationDetails([NotNull] TranslationResult translationResult, [NotNull] object translationEntryId)
        {
            TranslationEntryId = translationEntryId ?? throw new ArgumentNullException(nameof(translationEntryId));
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
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
        public PriorityWord GetWordInTranslationVariants(Guid correlationId)
        {
            foreach (var translationVariant in TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (translationVariant.CorrelationId == correlationId)
                    return translationVariant;

                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => synonym.CorrelationId == correlationId))
                    return synonym;
            }

            return null;
        }
    }
}