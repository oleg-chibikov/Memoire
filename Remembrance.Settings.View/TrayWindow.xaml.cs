using JetBrains.Annotations;
using Remembrance.ViewModel.Settings;
using Remembrance.Settings.View.Contracts;

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