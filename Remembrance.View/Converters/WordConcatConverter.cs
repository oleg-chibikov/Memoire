using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Converters;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(IReadOnlyCollection<TextEntry>), typeof(string))]
    sealed class WordConcatConverter : EnumerableConcatConverter
    {
        public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is IReadOnlyCollection<TextEntry> words))
            {
                return null;
            }

            var texts = words.Select(x => x.Text);
            return base.Convert(texts, targetType, parameter, culture);
        }
    }
}
