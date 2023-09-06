using System;
using System.Globalization;
using System.Windows.Data;
using Scar.Common.WPF.Localization;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(PartOfSpeech), typeof(string))]
    public sealed class PartOfSpeechLocalizedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is not PartOfSpeech
                ? null
                : CultureUtilities.GetLocalizedValue<string, WordMetadata>(value?.ToString() ??
                                                                           throw new InvalidOperationException(
                                                                               "Value.ToString() is null")) ?? value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
