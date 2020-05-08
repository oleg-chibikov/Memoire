using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Scar.Common.WPF.Converters;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.View.Converters
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
