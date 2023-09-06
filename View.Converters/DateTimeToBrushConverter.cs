using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(DateTimeOffset), typeof(Brush))]
    public sealed class DateTimeToBrushConverter : IValueConverter
    {
        readonly Brush _notReadyBrush = (Brush)Application.Current.FindResource("NotReadyToShowCardForeground");
        readonly Brush _readyBrush = (Brush)Application.Current.FindResource("ReadyToShowCardForeground");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value == null) || ((DateTimeOffset)value > DateTimeOffset.Now) ? _notReadyBrush : _readyBrush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
