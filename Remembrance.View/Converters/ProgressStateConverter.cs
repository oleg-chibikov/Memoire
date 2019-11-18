using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Shell;
using JetBrains.Annotations;
using Remembrance.Contracts.View;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(ProgressState), typeof(TaskbarItemProgressState))]
    internal sealed class ProgressStateConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return value == null ? TaskbarItemProgressState.None : (TaskbarItemProgressState)(int)value;
        }

        [NotNull]
        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}