using System.Windows;

namespace Remembrance.Resources.Buttons
{
    public partial class CloseButton
    {
        public CloseButton()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow((DependencyObject)sender)?.Close();
        }
    }
}