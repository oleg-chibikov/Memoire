using System;
using GalaSoft.MvvmLight;
using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public sealed class TranslationDetailsViewModel : ViewModelBase
    {
        private TranslationResultViewModel _translationResult;

        public TranslationDetailsViewModel([NotNull] TranslationResultViewModel translationResult)
        {
            TranslationResult = translationResult ?? throw new ArgumentNullException(nameof(translationResult));
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
            get => _translationResult;
            set { Set(() => TranslationResult, ref _translationResult, value); }
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