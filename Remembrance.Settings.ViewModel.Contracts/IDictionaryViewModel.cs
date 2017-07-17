using System.ComponentModel;
using System.Windows.Input;
using JetBrains.Annotations;
using Remembrance.Settings.ViewModel.Contracts.Data;

namespace Remembrance.Settings.ViewModel.Contracts
{
    public interface IDictionaryViewModel
    {
        [NotNull]
        Language[] AvailableSourceLanguages { get; }

        [NotNull]
        Language[] AvailableTargetLanguages { get; }

        [CanBeNull]
        string NewItemSource { get; }

        [NotNull]
        ICommand DeleteCommand { get; }

        [NotNull]
        ICommand OpenDetailsCommand { get; }

        [NotNull]
        ICommand OpenSettingsCommand { get; }

        [NotNull]
        ICommand SaveCommand { get; }

        [NotNull]
        ICommand SearchCommand { get; }

        [CanBeNull]
        string SearchText { get; }

        [NotNull]
        Language SelectedSourceLanguage { get; }

        [NotNull]
        Language SelectedTargetLanguage { get; }

        [NotNull]
        ICollectionView View { get; }

        int Count { get; }
    }
}