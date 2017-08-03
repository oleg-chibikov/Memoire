using System;
using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.ViewModel.Settings;

namespace Remembrance.Settings.View
{
    [UsedImplicitly]
    internal sealed partial class AddTranslationWindow : IAddTranslationWindow
    {
        public AddTranslationWindow([NotNull] AddTranslationViewModel viewModel)
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            //TODO: XAML only
            AddTranslationControl.WordTextBox.Focus();
        }
    }
}