using System;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel : BaseViewModel
    {
        readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsViewModel(
            Func<TranslationResult, TranslationEntry, TranslationResultViewModel> translationResultViewModelFactory,
            TranslationInfo translationInfo,
            ICommandManager commandManager) : base(commandManager)
        {
            _ = translationResultViewModelFactory ?? throw new ArgumentNullException(nameof(translationResultViewModelFactory));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _translationEntryKey = translationInfo.TranslationEntryKey;
            TranslationResult = translationResultViewModelFactory(translationInfo.TranslationDetails.TranslationResult, translationInfo.TranslationEntry);
        }

        public TranslationResultViewModel TranslationResult { get; }

        public override string ToString()
        {
            return $"Translation details for {_translationEntryKey}";
        }
    }
}
