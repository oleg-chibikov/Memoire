using System;
using System.Windows;
using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.ViewModel.Settings;

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