using System.Windows;

namespace Remembrance.Resources.Buttons
{
    public partial class MaximizeButton
    {
        public MaximizeButton()
        {
            InitializeComponent();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow((DependencyObject)sender);
            if (window != null)
                window.WindowState = WindowState.Maximized;
        }
    }
}