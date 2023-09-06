using System;
using System.Globalization;
using System.Windows.Data;
using Mémoire.Contracts.DAL.Model;
using Scar.Common;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(RepeatType), typeof(string))]
    public sealed class RepeatTypeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is not RepeatType status ? null : Convert(status);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        static string Convert(RepeatType repeatType)
        {
            return $"{repeatType}: {RepeatTypeSettings.RepeatTimes[repeatType].ToReadableFormat()}";
        }
    }
}
