using System;
using JetBrains.Annotations;
using Remembrance.ViewModel.Settings;
using Remembrance.Settings.View.Contracts;

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