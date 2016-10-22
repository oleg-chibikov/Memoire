using System;
using Common.Logging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.ViewModel.Contracts;
using Remembrance.Card.ViewModel.Contracts.Data;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.TypeAdapter.Contracts;
using Scar.Common.WPF.Localization;

namespace Remembrance.Card.ViewModel
{
    [UsedImplicitly]
    public class TranslationResultCardViewModel : ViewModelBase, ITranslationResultCardViewModel
    {
        [NotNull]
        private readonly ILog logger;

        private TranslationDetailsViewModel translationDetails;

        public TranslationResultCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessenger messenger,
            [NotNull] ILog logger)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.logger = logger;

            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, OnUiLanguageChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityChangeToken, OnPriorityChanged);
            TranslationDetails = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            Word = translationInfo.Key.Text;
        }

        public TranslationDetailsViewModel TranslationDetails
        {
            get { return translationDetails; }
            private set { Set(() => TranslationDetails, ref translationDetails, value); }
        }

        public string Word { get; }

        private void OnUiLanguageChanged([NotNull] string uiLanguage)
        {
            logger.Debug($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));
            CultureUtilities.ChangeCulture(uiLanguage);
            foreach (var partOfSpeechTranslation in TranslationDetails.TranslationResult.PartOfSpeechTranslations)
            {
                // ReSharper disable ExplicitCallerInfoArgument
                partOfSpeechTranslation.RaisePropertyChanged(nameof(partOfSpeechTranslation.PartOfSpeech));
                foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                {
                    translationVariant.RaisePropertyChanged(nameof(translationVariant.PartOfSpeech));
                    if (translationVariant.Synonyms != null)
                        foreach (var synonym in translationVariant.Synonyms)
                            synonym.RaisePropertyChanged(nameof(synonym.PartOfSpeech));
                    if (translationVariant.Meanings != null)
                        foreach (var meaning in translationVariant.Meanings)
                            meaning.RaisePropertyChanged(nameof(meaning.PartOfSpeech));
                }
                // ReSharper restore ExplicitCallerInfoArgument
            }
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));
            var parentId = priorityWordViewModel.ParentTranslationEntry?.Id ?? priorityWordViewModel.ParentTranslationDetails?.Id;
            if (parentId != TranslationDetails.Id)
                return;
            logger.Debug($"Priority changed for {priorityWordViewModel}. Updating the word in translation details...");
            var translation = TranslationDetails.GetWordInTranslationVariants(priorityWordViewModel.CorrelationId);
            if (translation != null)
            {
                logger.Debug($"Priority for {translation} is updated");
                translation.IsPriority = priorityWordViewModel.IsPriority;
            }
            else
            {
                logger.Debug("There is no matching translation in the card");
            }
        }
    }
}