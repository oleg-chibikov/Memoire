using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using JetBrains.Annotations;
using Scar.Common.Messages;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(MessageType), typeof(Color))]
    public sealed class MessageTypeToBrushConverter : IValueConverter
    {
        [NotNull]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((MessageType) value)
            {
                case MessageType.Message:
                    return Brushes.White;
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