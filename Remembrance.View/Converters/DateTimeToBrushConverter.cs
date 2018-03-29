using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(DateTime), typeof(Brush))]
    internal sealed class DateTimeToBrushConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return value == null || (DateTime)value > DateTime.Now ? Brushes.OrangeRed : Brushes.ForestGreen;
        }

        [NotNull]
        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}