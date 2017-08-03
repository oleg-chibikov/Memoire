using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;
using Remembrance.Contracts.DAL.Model;
using Scar.Common;

namespace Remembrance.Resources.Converters
{
    [ValueConversion(typeof(RepeatType), typeof(string))]
    public sealed class RepeatTypeConverter : IValueConverter
    {
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            var status = value as RepeatType?;
            return status == null
                ? null
                : Convert(status.Value);
        }

        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        [NotNull]
        private static string Convert(RepeatType repeatType)
        {
            return RepeatTypeSettings.RepeatTimes[repeatType].ToReadableFormat();
        }
    }
}