using System;
using System.Linq;
using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel
    {
        public TranslationDetailsViewModel([NotNull] TranslationResultViewModel translationResult)
        {
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
        }

        [DoNotNotify]
        public int Id
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public TranslationResultViewModel TranslationResult { get; set; }

        [CanBeNull]
        public PriorityWordViewModel GetWordInTranslationVariants(Guid correlationId)
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