using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using Scar.Common;
using Scar.Services.Contracts.Data.ExtendedTranslation;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(Reference), typeof(string))]
    public sealed class ExampleReferenceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not Reference reference)
            {
                return null;
            }

            var title = new StringBuilder(reference.Title);
            if (reference.OriginalTitle != null)
            {
                title.AppendFormat(CultureInfo.InvariantCulture, " ({0})", reference.OriginalTitle);
            }

            if (reference.Year != 0)
            {
                title.Append(" - ").Append(reference.Year);
            }

            var strings = new[]
            {
                reference.Type.CapitalizeIfNotEmpty(),
                reference.Author ?? reference.Director,
                title.ToString(),
                reference.ImdbLink
            };

            return string.Join(Environment.NewLine, strings.Where(x => x != null));
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
