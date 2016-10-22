using System;
using GalaSoft.MvvmLight;
using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public sealed class TranslationDetailsViewModel : ViewModelBase
    {
        private TranslationResultViewModel translationResult;

        public TranslationDetailsViewModel([NotNull] TranslationResultViewModel translationResult)
        {
            if (translationResult == null)
                throw new ArgumentNullException(nameof(translationResult));
            TranslationResult = translationResult;
        }

        public int Id
        {
            get;
            [UsedImplicitly]
            set;
        }

        [NotNull]
        public TranslationResultViewModel TranslationResult
        {
            get { return translationResult; }
            set { Set(() => TranslationResult, ref translationResult, value); }
        }

        [CanBeNull]
        public PriorityWordViewModel GetWordInTranslationVariants(Guid correlationId)
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