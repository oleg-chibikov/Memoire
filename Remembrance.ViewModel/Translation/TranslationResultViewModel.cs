using JetBrains.Annotations;
using PropertyChanged;

namespace Remembrance.ViewModel.Translation
{
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public sealed class TranslationResultViewModel
    {
        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations { get; set; }
    }
}