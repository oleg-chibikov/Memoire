using System.Windows.Input;
using JetBrains.Annotations;

namespace Remembrance.Settings.ViewModel.Contracts
{
    public interface ITrayViewModel
    {
        bool IsActive { get; }

        [NotNull]
        ICommand AddTranslationCommand { get; }

        [NotNull]
        ICommand ShowDictionaryCommand { get; }

        [NotNull]
        ICommand ShowSettingsCommand { get; }

        [NotNull]
        ICommand ToggleActiveCommand { get; }

        [NotNull]
        ICommand ExitCommand { get; }
    }
}