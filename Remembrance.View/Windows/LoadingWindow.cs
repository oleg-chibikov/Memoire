using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;

namespace Remembrance.View.Windows
{
    [UsedImplicitly]
    internal sealed partial class LoadingWindow : ILoadingWindow
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }
    }
}