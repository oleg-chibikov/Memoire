using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(bool), typeof(ResizeMode))]
    public sealed class BoolToResizeModeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool and true ? ResizeMode.CanResize : ResizeMode.NoResize;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
