using System.Windows;
using System.Windows.Data;
using Scar.Common.WPF.Converters;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.View.Converters
{
    [ValueConversion(typeof(PartOfSpeech), typeof(Visibility))]
    public sealed class PartOfSpeechToVisibilityConverter : ValueToVisibilityConverter<PartOfSpeech?>
    {
        protected override bool IsVisible(PartOfSpeech? value)
        {
            return (value != null) && (value != PartOfSpeech.Unknown);
        }
    }
}
