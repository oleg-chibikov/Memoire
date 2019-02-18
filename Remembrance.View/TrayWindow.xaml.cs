using System.Threading.Tasks;
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
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public TrayWindow([NotNull] TrayViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            LoadingTask = _taskCompletionSource.Task;
            Loaded += TrayWindow_Loaded;
        }

        private void TrayWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _taskCompletionSource.SetResult(true);
        }

        public Task LoadingTask { get; }
    }
}