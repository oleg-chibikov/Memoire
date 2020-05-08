using System;
using System.Globalization;
using System.Windows.Data;
using Mémoire.ViewModel;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(WordViewModel), typeof(bool))]
    sealed class PriorityWordViewModelToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is PriorityWordViewModel;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
