using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Localization;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(PartOfSpeech), typeof(string))]
    public sealed class PartOfSpeechLocalizedConverter : IValueConverter
    {
        public object Convert(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            return !(value is PartOfSpeech)
                ? null
                : CultureUtilities.GetLocalizedValue<string, WordMetadata>(value.ToString());
        }

        public object ConvertBack(object value, [NotNull] Type targetType, object parameter, [NotNull] CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}