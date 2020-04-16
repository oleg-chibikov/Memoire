using System;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
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
