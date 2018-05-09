using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;

namespace Remembrance.View.Settings
{
    /// <summary>
    /// The loading window.
    /// </summary>
    [UsedImplicitly]
    internal sealed partial class LoadingWindow : ILoadingWindow
    {
        public LoadingWindow()
        {
            InitializeComponent();
        }
    }
}