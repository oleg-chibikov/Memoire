using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(string), typeof(Span))]
    sealed class TextToSpanConverter : IValueConverter
    {
        static readonly char[] Separators = new[]
        {
            '<',
            '>'
        };

        readonly double _fontSize;
        readonly Brush _matchForeground;

        public TextToSpanConverter()
        {
            _fontSize = (double)Application.Current.FindResource("BigFontSize");
            _matchForeground = (Brush)Application.Current.FindResource("MoreExamplesMatchForeground");
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
#pragma warning disable IDE0019 // Use pattern matching
            var stringValue = value as string;
#pragma warning restore IDE0019 // Use pattern matching
            if (stringValue == null)
            {
                return null;
            }

            var isInsideTag = stringValue.StartsWith("<", StringComparison.Ordinal);
            var parts = stringValue.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            var span = new Span();

            foreach (var part in parts)
            {
                var run = new Run(part);
                if (isInsideTag)
                {
                    run.FontSize = _fontSize;
                    run.Foreground = _matchForeground;
                }

                isInsideTag = !isInsideTag;

                span.Inlines.Add(run);
            }

            return span;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
