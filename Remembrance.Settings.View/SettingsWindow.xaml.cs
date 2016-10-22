using System;
using System.Windows;
using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;

namespace Remembrance.Settings.View
{
    [UsedImplicitly]
    internal partial class SettingsWindow : ISettingsWindow
    {
        public SettingsWindow([NotNull] ISettingsViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));
            Owner = ownerWindow;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}