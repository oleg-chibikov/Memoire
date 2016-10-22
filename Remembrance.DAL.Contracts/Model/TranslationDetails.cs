using System;
using JetBrains.Annotations;
using Remembrance.Translate.Contracts.Data.WordsTranslator;

namespace Remembrance.DAL.Contracts.Model
{
    public sealed class TranslationDetails : Entity
    {
        [UsedImplicitly]
        // ReSharper disable once NotNullMemberIsNotInitialized
        public TranslationDetails()
        {
        }

        public TranslationDetails([NotNull] TranslationResult translationResult)
        {
            if (translationResult == null)
                throw new ArgumentNullException(nameof(translationResult));
            TranslationResult = translationResult;
        }

        [NotNull]
        public TranslationResult TranslationResult { get; set; }

        [CanBeNull]
        public PriorityWord GetWordInTranslationVariants(Guid correlationId)
        {
            foreach (var partOfSpeechTranslation in TranslationResult.PartOfSpeechTranslations)
            foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
            {
                if (translationVariant.CorrelationId == correlationId)
                    return translationVariant;
                if (translationVariant.Synonyms == null)
                    continue;
                foreach (var synonym in translationVariant.Synonyms)
                    if (synonym.CorrelationId == correlationId)
                        return synonym;
            }
            return null;
        }
    }
}