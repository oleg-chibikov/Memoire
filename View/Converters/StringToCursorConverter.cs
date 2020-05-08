using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(string), typeof(Cursor))]
    sealed class StringToCursorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value == null ? Cursors.Help : Cursors.Hand;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
