using System;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel
    {
        private readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsViewModel(
            [NotNull] Func<TranslationResult, TranslationEntry, TranslationResultViewModel> translationResultViewModelFactory,
            [NotNull] TranslationInfo translationInfo)
        {
            _ = translationResultViewModelFactory ?? throw new ArgumentNullException(nameof(translationResultViewModelFactory));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _translationEntryKey = translationInfo.TranslationEntryKey;
            TranslationResult = translationResultViewModelFactory(translationInfo.TranslationDetails.TranslationResult, translationInfo.TranslationEntry);
        }

        [NotNull]
        public TranslationResultViewModel TranslationResult { get; }

        public override string ToString()
        {
            return $"Translation details for {_translationEntryKey}";
        }
    }
}