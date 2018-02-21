using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Converters;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(ICollection<TextEntry>), typeof(string))]
    public sealed class WordConcatConverter : EnumerableConcatConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ICollection<TextEntry> words))
            {
                return null;
            }

            var texts = words.Select(x => x.Text);
            return base.Convert(texts, targetType, parameter, culture);
        }
    }
}