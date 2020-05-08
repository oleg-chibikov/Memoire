using System;
using System.Windows;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
{
    sealed partial class SettingsWindow : ISettingsWindow
    {
        public SettingsWindow(SettingsViewModel viewModel, Window? ownerWindow = null)
        {
            Owner = ownerWindow;
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
