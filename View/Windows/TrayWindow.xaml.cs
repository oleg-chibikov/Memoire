using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    public sealed partial class TrayWindow : ITrayWindow
    {
        public TrayWindow(TrayViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
