using JetBrains.Annotations;
using Remembrance.Settings.View.Contracts;

namespace Remembrance.Settings.View
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