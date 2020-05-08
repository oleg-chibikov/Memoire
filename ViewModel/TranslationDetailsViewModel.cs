using System;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationDetailsViewModel : BaseViewModel
    {
        readonly TranslationEntryKey _translationEntryKey;

        public TranslationDetailsViewModel(
            Func<TranslationInfo, TranslationResultViewModel> translationResultViewModelFactory,
            TranslationInfo translationInfo,
            ICommandManager commandManager) : base(commandManager)
        {
            _ = translationResultViewModelFactory ?? throw new ArgumentNullException(nameof(translationResultViewModelFactory));
            _ = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _translationEntryKey = translationInfo.TranslationEntryKey;
            TranslationResult = translationResultViewModelFactory(translationInfo);
        }

        public TranslationResultViewModel TranslationResult { get; }

        public override string ToString()
        {
            return $"Translation details for {_translationEntryKey}";
        }
    }
}
