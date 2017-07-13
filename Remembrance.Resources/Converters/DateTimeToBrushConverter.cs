using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Remembrance.Resources.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    public class DateTimeToBrushConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return value == null || (DateTime)value > DateTime.Now
                ? Brushes.OrangeRed
                : Brushes.ForestGreen;
        }

        [NotNull]
        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}