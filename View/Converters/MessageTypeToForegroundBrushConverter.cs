using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Scar.Common.Messages;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(MessageType), typeof(Color))]
    sealed class MessageTypeToForegroundBrushConverter : IValueConverter
    {
        readonly Brush _messageForeground;
        readonly Brush _warningForeground;
        readonly Brush _errorForeground;

        public MessageTypeToForegroundBrushConverter()
        {
            _messageForeground = (Brush)Application.Current.FindResource("Foreground");
            _warningForeground = (Brush)Application.Current.FindResource("WarningForeground");
            _errorForeground = (Brush)Application.Current.FindResource("ErrorForeground");
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return _messageForeground;
            }

            return ((MessageType)value) switch
            {
                MessageType.Message => _messageForeground,
                MessageType.Warning => _warningForeground,
                MessageType.Error => _errorForeground,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
