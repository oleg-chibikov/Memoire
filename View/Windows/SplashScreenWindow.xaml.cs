using System;
using System.Threading;
using System.Windows;
using Mémoire.Contracts.View.Settings;

namespace Mémoire.View.Windows
{
    sealed partial class SplashScreenWindow : ISplashScreenWindow
    {
        Timer? _timer;

        public SplashScreenWindow()
        {
            InitializeComponent();
            Loaded += SplashScreenWindow_Loaded;
        }

        protected override void Dispose(bool disposing)
        {
            _timer?.Dispose();
            base.Dispose(disposing);
        }

        static string AppendDots(string text)
        {
            var indexOfDots = text.IndexOf("...", StringComparison.Ordinal);
            if (indexOfDots != -1)
            {
                text = text.Substring(0, indexOfDots);
            }
            else
            {
                text += ".";
            }

            return text;
        }

        void SplashScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var syncContext = SynchronizationContext.Current;
            _timer = new Timer(
                x =>
                {
                    syncContext?.Post(
                        state =>
                        {
                            var text = MainText.Text;
                            text = AppendDots(text);
                            MainText.Text = text;
                            MainText.UpdateLayout();
                        },
                        null);
                },
                null,
                1000,
                1000);
        }
    }
}
