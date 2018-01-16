using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.ViewModel.Translation
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultViewModel
    {
        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations { get; set; }
    }
}