using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(RepeatType), typeof(string))]
    internal sealed class RepeatTypeConverter : IValueConverter
    {
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return !(value is RepeatType status) ? null : Convert(status);
        }

        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        [NotNull]
        private static string Convert(RepeatType repeatType)
        {
            return $"{repeatType}: {RepeatTypeSettings.RepeatTimes[repeatType].ToReadableFormat()}";
        }
    }
}