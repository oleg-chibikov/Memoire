using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.ViewModel.Settings.Data;
using Remembrance.ViewModel.Translation;
using Scar.Common.WPF.Localization;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class TranslationResultCardViewModel : IDisposable
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public TranslationResultCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] ILog logger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IMessageHub messenger)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));

            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            TranslationDetails = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            Word = translationInfo.Key.Text;

            _subscriptionTokens.Add(messenger.Subscribe<Language>(OnUiLanguageChanged));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordViewModel>(OnPriorityChanged));
        }

        [NotNull]
        public TranslationDetailsViewModel TranslationDetails { get; }

        [NotNull]
        public string Word { get; }

        public void Dispose()
        {
            foreach (var token in _subscriptionTokens)
                _messenger.UnSubscribe(token);
        }

        [CanBeNull]
        private PriorityWordViewModel GetWordInTranslationDetails(IWord word)
        {
            foreach (var translationVariant in TranslationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (_wordsEqualityComparer.Equals(translationVariant, word))
                    return translationVariant;

                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => _wordsEqualityComparer.Equals(synonym, word)))
                    return synonym;
            }

            return null;
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            if (!priorityWordViewModel.TranslationEntryId.Equals(TranslationDetails.TranslationEntryId))
                return;

            _logger.Trace($"Priority changed for {priorityWordViewModel}. Updating the word in translation details...");
            var translation = GetWordInTranslationDetails(priorityWordViewModel);
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

        private void OnUiLanguageChanged([NotNull] Language uiLanguage)
        {
            _logger.Trace($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage.Code);

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