using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Shell;
using Mémoire.Contracts.View;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(ProgressState), typeof(TaskbarItemProgressState))]
    public sealed class ProgressStateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value == null ? TaskbarItemProgressState.None : (TaskbarItemProgressState)(int)value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
