using System;
using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    sealed partial class AddTranslationWindow : IAddTranslationWindow
    {
        public AddTranslationWindow(AddTranslationViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
