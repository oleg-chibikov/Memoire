using GalaSoft.MvvmLight;
using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    // ReSharper disable NotNullMemberIsNotInitialized
    public class TranslationResultViewModel : ViewModelBase
    {
        private PartOfSpeechTranslationViewModel[] partOfSpeechTranslations;

        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations
        {
            get { return partOfSpeechTranslations; }
            set { Set(() => PartOfSpeechTranslations, ref partOfSpeechTranslations, value); }
        }
    }
}