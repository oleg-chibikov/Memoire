using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Remembrance.Contracts;
using Scar.Common.WPF.Converters;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(IWord[]), typeof(string))]
    public sealed class WordConcatConverter : ArrayConcatConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is IWord[] words))
            {
                return null;
            }

            var texts = words.Select(x => x.WordText).ToArray();
            return base.Convert(texts, targetType, parameter, culture);
        }
    }
}