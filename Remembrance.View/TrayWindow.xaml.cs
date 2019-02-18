using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View
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