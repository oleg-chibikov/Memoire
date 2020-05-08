using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    sealed class HasManualTranslationsToBrushConverter : IValueConverter
    {
        readonly Brush _hasTranslationsForeground;
        readonly Brush _noTranslationsForeground;

        public HasManualTranslationsToBrushConverter()
        {
            _hasTranslationsForeground = (Brush)Application.Current.FindResource("HasManualTranslationsForeground");
            _noTranslationsForeground = (Brush)Application.Current.FindResource("Foreground");
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value == null) || (bool)value ? _hasTranslationsForeground : _noTranslationsForeground;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
