using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Converters;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(TextEntry[]), typeof(string))]
    public sealed class WordConcatConverter : ArrayConcatConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TextEntry[] words))
            {
                return null;
            }

            var texts = words.Select(x => x.Text).ToArray();
            return base.Convert(texts, targetType, parameter, culture);
        }
    }
}