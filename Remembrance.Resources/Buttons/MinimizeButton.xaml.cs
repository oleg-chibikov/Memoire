using System.Windows;

namespace Remembrance.Resources.Buttons
{
    public partial class MinimizeButton
    {
        public MinimizeButton()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }
    }
}