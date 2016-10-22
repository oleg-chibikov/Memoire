using System.Windows;

namespace Remembrance.Resources.Buttons
{
    public partial class RestoreButton
    {
        public RestoreButton()
        {
            InitializeComponent();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            if (window != null)
                window.WindowState = WindowState.Normal;
        }
    }
}