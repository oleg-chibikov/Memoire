using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Scar.Common.Messages;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(MessageType), typeof(Color))]
    sealed class MessageTypeToBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return new SolidColorBrush(Color.FromArgb(255, 54, 54, 54));
            }

            switch ((MessageType)value)
            {
                case MessageType.Message:
                    return new SolidColorBrush(Color.FromArgb(255, 54, 54, 54));
                case MessageType.Warning:
                    return Brushes.Wheat;
                case MessageType.Error:
                    return Brushes.OrangeRed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
