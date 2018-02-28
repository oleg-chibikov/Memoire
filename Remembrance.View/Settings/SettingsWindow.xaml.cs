using System;
using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel.Settings;

namespace Remembrance.View.Settings
{
    /// <summary>
    /// The settings window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class SettingsWindow : ISettingsWindow
    {
        public SettingsWindow([NotNull] SettingsViewModel viewModel, [CanBeNull] Window ownerWindow = null)
        {
            Owner = ownerWindow;
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
        }
    }
}