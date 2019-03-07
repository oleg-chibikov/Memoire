using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JetBrains.Annotations;
using Scar.Common.Messages;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(MessageType), typeof(Color))]
    internal sealed class MessageTypeToBackgroundBrushConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
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