using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations { get; set; }
    }
}