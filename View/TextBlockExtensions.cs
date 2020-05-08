using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Mémoire.View
{
    public static class TextBlockExtensions
    {
        public static readonly DependencyProperty FormattedTextProperty = DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(Inline),
            typeof(TextBlockExtensions),
            new PropertyMetadata(null, OnFormattedTextChanged));

        public static Inline GetFormattedText(DependencyObject obj)
        {
            _ = obj ?? throw new ArgumentNullException(nameof(obj));

            return (Inline)obj.GetValue(FormattedTextProperty);
        }

        public static void SetFormattedText(DependencyObject obj, Inline value)
        {
            _ = obj ?? throw new ArgumentNullException(nameof(obj));

            obj.SetValue(FormattedTextProperty, value);
        }

        static void OnFormattedTextChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (!(o is TextBlock textBlock))
            {
                return;
            }

            var inline = (Inline)e.NewValue;
            textBlock.Inlines.Clear();
            if (inline != null)
            {
                textBlock.Inlines.Add(inline);
            }
        }
    }
}
