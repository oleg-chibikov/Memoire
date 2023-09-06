using System;
using System.Windows;
using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    public sealed partial class SettingsWindow : ISettingsWindow
    {
        public SettingsWindow(SettingsViewModel viewModel, Window? ownerWindow = null)
        {
            Owner = ownerWindow;
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}
