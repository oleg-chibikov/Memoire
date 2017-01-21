using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Remembrance.Resources.Buttons
{
    public class ImageButton : Button
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageButton), new PropertyMetadata(null));

        public ImageButton()
        {
            var binding = new Binding(nameof(Source)) { Source = this };
            var image = new Image { Stretch = Stretch.Fill, Width = 25, Height = 25 };
            image.SetBinding(Image.SourceProperty, binding);
            Focusable = false;
            IsTabStop = false;
            Background = Brushes.Transparent;
            Cursor = Cursors.Hand;
            Padding = new Thickness();
            BorderThickness = new Thickness();
            Content = image;
        }

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }
    }
}