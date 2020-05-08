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
    public sealed class TranslationResultViewModel : BaseViewModel, IWithExtendedExamples
    {
        readonly Action _loadExtendedExamples;
        bool _isExpanded;

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

            var allTranslationVariantsHashSet = new HashSet<BaseWord>(GetAllTranslationVariantsWithSynonyms(translationInfo));
            var examples = translationInfo.TranslationDetails.ExtendedTranslationResult?.ExtendedPartOfSpeechTranslations.Where(
                x => x.Translation.Text == null || !allTranslationVariantsHashSet.Contains(x.Translation)).ToArray();
            HasExtendedExamples = examples?.Length > 0;

            _loadExtendedExamples = () =>
            {
                ExtendedExamples = examples.SelectMany(x => x.ExtendedExamples).ToArray();
            };
        }

        public IReadOnlyCollection<PartOfSpeechTranslationViewModel> PartOfSpeechTranslations { get; }

        public IReadOnlyCollection<ExtendedExample>? ExtendedExamples { get; private set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                if (value && ExtendedExamples == null)
                {
                    _loadExtendedExamples();
                }
            }
        }

        public bool HasExtendedExamples { get; set; }

        static IEnumerable<Word> GetAllTranslationVariantsWithSynonyms(TranslationInfo translationInfo)
        {
            return translationInfo.TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(
                partOfSpeechTranslationViewModel => partOfSpeechTranslationViewModel.TranslationVariants.SelectMany(TranslationVariantExtensions.GetTranslationVariantAndSynonyms));
        }
    }
}
