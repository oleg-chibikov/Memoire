using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    internal sealed class DateTimeToBrushConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return value == null || (DateTime)value > DateTime.Now ? Brushes.OrangeRed : Brushes.MediumSeaGreen;
        }

        [NotNull]
        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}