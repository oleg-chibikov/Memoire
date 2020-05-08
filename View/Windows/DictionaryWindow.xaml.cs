using System;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class DictionaryWindow : IDictionaryWindow
    {
        public DictionaryWindow(DictionaryViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
