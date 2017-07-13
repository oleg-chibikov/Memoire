using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;

namespace Remembrance.Settings.View
{
    [UsedImplicitly]
    internal sealed partial class DictionaryWindow : IDictionaryWindow
    {
        public DictionaryWindow([NotNull] IDictionaryViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            AddTranslationControl.WordTextBox.Focus();
        }
    }
}