using GalaSoft.MvvmLight;
using JetBrains.Annotations;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    // ReSharper disable NotNullMemberIsNotInitialized
    public class TranslationResultViewModel : ViewModelBase
    {
        private PartOfSpeechTranslationViewModel[] _partOfSpeechTranslations;

        [NotNull]
        public PartOfSpeechTranslationViewModel[] PartOfSpeechTranslations
        {
            get => _partOfSpeechTranslations;
            set { Set(() => PartOfSpeechTranslations, ref _partOfSpeechTranslations, value); }
        }
    }
}