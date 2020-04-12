using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Remembrance.ViewModel;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(WordViewModel), typeof(Visibility))]
    internal sealed class PriorityWordViewModelToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !(value is PriorityWordViewModel) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
