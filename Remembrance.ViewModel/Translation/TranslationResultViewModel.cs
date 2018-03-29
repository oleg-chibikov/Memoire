using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        public TranslationResultViewModel(
            [NotNull] TranslationResult translationResult,
            [NotNull] TranslationEntry translationEntry,
            [NotNull] Func<PartOfSpeechTranslation, TranslationEntry, PartOfSpeechTranslationViewModel> partOfSpeechTranslationViewModelFactory)
        {
            if (partOfSpeechTranslationViewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(partOfSpeechTranslationViewModelFactory));
            }

            if (translationResult == null)
            {
                throw new ArgumentNullException(nameof(translationResult));
            }

            PartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.Select(partOfSpeechTranslation => partOfSpeechTranslationViewModelFactory(partOfSpeechTranslation, translationEntry)).ToArray();
        }

        [NotNull]
        public ICollection<PartOfSpeechTranslationViewModel> PartOfSpeechTranslations { get; }
    }
}