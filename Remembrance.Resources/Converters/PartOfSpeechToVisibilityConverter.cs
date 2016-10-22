using System.Windows;
using System.Windows.Data;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Scar.Common.WPF.Converters;

namespace Remembrance.Resources.Converters
{
    [ValueConversion(typeof(PartOfSpeech), typeof(Visibility))]
    public sealed class PartOfSpeechToVisibilityConverter : ValueToVisibilityConverter<PartOfSpeech?>
    {
        protected override bool IsVisible(PartOfSpeech? value) => value != null && value != PartOfSpeech.Unknown;
    }
}