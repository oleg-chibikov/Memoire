using System;
using System.Windows;
using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
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