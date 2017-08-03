using System;
using System.Windows;
using JetBrains.Annotations;
using Remembrance.ViewModel.Settings;
using Remembrance.Settings.View.Contracts;

namespace Remembrance.Settings.View
{
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