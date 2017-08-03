using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.Card.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations { get; set; }
    }
}