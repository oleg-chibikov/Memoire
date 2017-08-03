using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations { get; set; }
    }
}