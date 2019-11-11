using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(byte[]), typeof(BitmapImage))]
    internal sealed class BytesToImageConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            if (!(value is byte[] imageBytes))
            {
                return null;
            }

            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageBytes))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        [NotNull]
        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}