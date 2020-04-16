using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel;

namespace Remembrance.View.Windows
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
