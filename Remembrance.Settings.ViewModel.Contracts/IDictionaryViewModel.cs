using System.ComponentModel;
using System.Windows.Input;
using JetBrains.Annotations;

namespace Remembrance.Settings.ViewModel.Contracts
{
    public interface IDictionaryViewModel : IWithAddTranslationControl
    {
        [CanBeNull]
        string NewItemSource { get; }

        [NotNull]
        ICommand DeleteCommand { get; }

        [NotNull]
        ICommand OpenDetailsCommand { get; }

        [NotNull]
        ICommand OpenSettingsCommand { get; }

        [NotNull]
        ICommand SearchCommand { get; }

        [CanBeNull]
        string SearchText { get; }

        [NotNull]
        ICollectionView View { get; }

        int Count { get; }
    }
}