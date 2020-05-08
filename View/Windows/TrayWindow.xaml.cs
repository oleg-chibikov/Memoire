using Mémoire.Contracts.View.Settings;
using Mémoire.ViewModel;

namespace Mémoire.View.Windows
{
    sealed partial class TrayWindow : ITrayWindow
    {
        public TrayWindow(TrayViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
