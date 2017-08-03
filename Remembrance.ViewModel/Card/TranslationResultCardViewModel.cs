using System;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.TypeAdapter;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common.WPF.Localization;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultCardViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        public TranslationResultCardViewModel([NotNull] TranslationInfo translationInfo, [NotNull] IViewModelAdapter viewModelAdapter, [NotNull] IMessenger messenger, [NotNull] ILog logger)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            TranslationDetails = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            Word = translationInfo.Key.Text;

            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, OnUiLanguageChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityChangeToken, OnPriorityChanged);
        }

        public TranslationDetailsViewModel TranslationDetails { get; }

        public string Word { get; }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.ParentTranslationEntryViewModel?.Id ?? priorityWordViewModel.ParentTranslationDetailsViewModel?.TranslationEntryId;
            if (parentId != TranslationDetails.TranslationEntryId)
                return;

            _logger.Trace($"Priority changed for {priorityWordViewModel}. Updating the word in translation details...");
            var translation = TranslationDetails.GetWordInTranslationVariants(priorityWordViewModel.CorrelationId);
            if (translation != null)
            {
                _logger.Trace($"Priority for {translation} is updated");
                translation.IsPriority = priorityWordViewModel.IsPriority;
            }
            else
            {
                _logger.Trace("There is no matching translation in the card");
            }
        }

        private void OnUiLanguageChanged([NotNull] string uiLanguage)
        {
            _logger.Trace($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage);

            foreach (var partOfSpeechTranslation in TranslationDetails.TranslationResult.PartOfSpeechTranslations)
            {
                partOfSpeechTranslation.ReRender();
                foreach (var translationVariant in partOfSpeechTranslation.TranslationVariants)
                {
                    translationVariant.ReRender();
                    if (translationVariant.Synonyms != null)
                        foreach (var synonym in translationVariant.Synonyms)
                            synonym.ReRender();
                    if (translationVariant.Meanings != null)
                        foreach (var meaning in translationVariant.Meanings)
                            meaning.ReRender();
                }
            }
        }
    }
}