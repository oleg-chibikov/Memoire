using JetBrains.Annotations;
using Remembrance.Contracts.View.Settings;

namespace Remembrance.View.Settings
{
    [UsedImplicitly]
    internal sealed partial class SplashScreenWindow : ISplashScreenWindow
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }
    }
}