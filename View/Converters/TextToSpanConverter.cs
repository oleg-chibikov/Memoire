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
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || !(value is string stringValue))
            {
                return null;
            }

            var isInsideTag = stringValue.StartsWith("<", StringComparison.Ordinal);
            var parts = stringValue.Split('<', '>');
            var span = new Span();

            foreach (var part in parts)
            {
                var run = new Run(part);
                if (isInsideTag)
                {
                    run.FontSize = (double)Application.Current.Resources["BigFontSize"];
                    run.Foreground = (Brush)Application.Current.Resources["MoreExamplesMatchForeground"];
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
