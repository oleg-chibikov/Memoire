using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;
using Remembrance.ViewModel.Settings;

namespace Remembrance.Settings.View
{
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