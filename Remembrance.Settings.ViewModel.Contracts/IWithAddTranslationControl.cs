using System.Windows.Input;
using JetBrains.Annotations;
using Remembrance.Settings.ViewModel.Contracts.Data;

namespace Remembrance.Settings.ViewModel.Contracts
{
    public interface IWithAddTranslationControl
    {
        [NotNull]
        Language[] AvailableSourceLanguages { get; }

        [NotNull]
        Language[] AvailableTargetLanguages { get; }

        [NotNull]
        ICommand SaveCommand { get; }

        [NotNull]
        Language SelectedSourceLanguage { get; }

        [NotNull]
        Language SelectedTargetLanguage { get; }
    }
}