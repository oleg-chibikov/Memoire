using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel.Settings;

namespace Remembrance.View.Settings
{
    /// <summary>
    /// The tray window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class TrayWindow : ITrayWindow
    {
        public TrayWindow([NotNull] TrayViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}