using System.Windows;
using System.Windows.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.WPF.Converters;

namespace Remembrance.View.Converters
{
    [ValueConversion(typeof(PartOfSpeech), typeof(Visibility))]
    sealed class PartOfSpeechToVisibilityConverter : ValueToVisibilityConverter<PartOfSpeech?>
    {
        protected override bool IsVisible(PartOfSpeech? value)
        {
            return (value != null) && (value != PartOfSpeech.Unknown);
        }
    }
}
