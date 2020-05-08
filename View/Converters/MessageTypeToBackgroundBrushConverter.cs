using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Scar.Common.Messages;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(MessageType), typeof(Color))]
    sealed class MessageTypeToBackgroundBrushConverter : IValueConverter
    {
        readonly Brush _messageBackground;
        readonly Brush _warningBackground;
        readonly Brush _errorBackground;

        public MessageTypeToBackgroundBrushConverter()
        {
            _messageBackground = (Brush)Application.Current.FindResource("Background");
            _warningBackground = (Brush)Application.Current.FindResource("WarningBackground");
            _errorBackground = (Brush)Application.Current.FindResource("ErrorBackground");
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return _messageBackground;
            }

            return ((MessageType)value) switch
            {
                MessageType.Message => _messageBackground,
                MessageType.Warning => _warningBackground,
                MessageType.Error => _errorBackground,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
