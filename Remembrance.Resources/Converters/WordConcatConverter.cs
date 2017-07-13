using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Scar.Common.WPF.Converters;

namespace Remembrance.Resources.Converters
{
    [ValueConversion(typeof(dynamic[]), typeof(string))]
    public sealed class WordConcatConverter : ArrayConcatConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var words = value as dynamic[];
            if (words == null)
                return null;

            var texts = words.Select(x => (string)x.Text).ToArray();
            return base.Convert(texts, targetType, parameter, culture);
        }
    }
}