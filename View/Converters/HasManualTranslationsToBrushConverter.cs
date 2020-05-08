using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    sealed class HasManualTranslationsToBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value == null) || (bool)value ? Brushes.MediumSeaGreen : Brushes.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
