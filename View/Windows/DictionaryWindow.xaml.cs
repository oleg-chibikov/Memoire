using System;
using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    public sealed partial class DictionaryWindow : IDictionaryWindow
    {
        public DictionaryWindow(DictionaryViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
