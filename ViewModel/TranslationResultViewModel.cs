using System;
using System.Collections.Generic;
using System.Linq;
using Mémoire.Contracts.Processing.Data;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts.Data.ExtendedTranslation;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel : BaseViewModel
    {
        public TranslationResultViewModel(
            TranslationInfo translationInfo,
            Func<PartOfSpeechTranslation, TranslationInfo, PartOfSpeechTranslationViewModel> partOfSpeechTranslationViewModelFactory,
            ICommandManager commandManager) : base(commandManager)
        {
            _ = partOfSpeechTranslationViewModelFactory ?? throw new ArgumentNullException(nameof(partOfSpeechTranslationViewModelFactory));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            PartOfSpeechTranslations = translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations
                .Select(partOfSpeechTranslation => partOfSpeechTranslationViewModelFactory(partOfSpeechTranslation, translationInfo))
                .ToArray();
            ExtendedExamplesViewModel = new ExtendedExamplesViewModel(translationInfo, GetExamples, commandManager);
        }

        public ExtendedExamplesViewModel ExtendedExamplesViewModel { get; }

        public IReadOnlyCollection<PartOfSpeechTranslationViewModel> PartOfSpeechTranslations { get; }

        static IReadOnlyCollection<ExtendedPartOfSpeechTranslation>? GetExamples(TranslationInfo translationInfo)
        {
            var allTranslationVariantsHashSet = new HashSet<BaseWord>(GetAllTranslationVariantsWithSynonyms(translationInfo));
            return translationInfo.TranslationDetails.ExtendedTranslationResult?.ExtendedPartOfSpeechTranslations.Where(
                    x => x.Translation.Text == null || !allTranslationVariantsHashSet.Contains(x.Translation))
                .ToArray();
        }

        static IEnumerable<Word> GetAllTranslationVariantsWithSynonyms(TranslationInfo translationInfo)
        {
            return translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(
                partOfSpeechTranslationViewModel => partOfSpeechTranslationViewModel.TranslationVariants.SelectMany(TranslationVariantExtensions.GetTranslationVariantAndSynonyms));
        }
    }
}
