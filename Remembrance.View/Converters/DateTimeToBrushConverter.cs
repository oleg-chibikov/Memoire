using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    sealed class DateTimeToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value == null || (DateTime)value > DateTime.Now ? Brushes.OrangeRed : Brushes.MediumSeaGreen;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
