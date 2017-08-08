using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Translation
{
    //TODO: Simplify Model
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public class PriorityWordViewModel : WordViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly ViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ViewModelAdapter viewModelAdapter,
            [NotNull] IMessenger messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TogglePriorityCommand = new CorrelationCommand(TogglePriority);
        }

        #region Commands

        public ICommand TogglePriorityCommand { get; }

        #endregion

        public override bool CanEdit { get; } = true;

        public bool IsPriority { get; set; }

        public object TranslationEntryId { get; set; }

        private void Add([NotNull] TranslationEntry translationEntry)
        {
            _logger.Info($"Adding {this} to {translationEntry}...");
            var priorityWord = _viewModelAdapter.Adapt<PriorityWord>(this);
            translationEntry.Translations.Add(priorityWord);
            _messenger.Send(GetCurrentCopy(translationEntry), MessengerTokens.PriorityAddToken);
        }

        /// <summary>
        /// Get a copy of translationEntry in order to prevent links break in external handlers
        /// </summary>
        private PriorityWordViewModel GetCurrentCopy([NotNull] TranslationEntry translationEntry)
        {
            var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationEntry);
            var word = translationEntryViewModel.Translations.Single(x => _wordsEqualityComparer.Equals(x, this));
            return word;
        }

        [CanBeNull]
        private PriorityWord GetWordInTranslationDetails([NotNull] TranslationDetails translationDetails)
        {
            foreach (var translationVariant in translationDetails.TranslationResult.PartOfSpeechTranslations.SelectMany(partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants))
            {
                if (_wordsEqualityComparer.Equals(translationVariant, this))
                    return translationVariant;

                if (translationVariant.Synonyms == null)
                    continue;

                foreach (var synonym in translationVariant.Synonyms.Where(synonym => _wordsEqualityComparer.Equals(synonym, this)))
                    return synonym;
            }

            return null;
        }

        [CanBeNull]
        private PriorityWord GetWordInTranslationEntry([NotNull] TranslationEntry translationEntry)
        {
            return translationEntry.Translations.SingleOrDefault(x => _wordsEqualityComparer.Equals(x, this));
        }

        private void Remove([NotNull] PriorityWord wordInTranslationEntry, [NotNull] TranslationEntry translationEntry, [NotNull] TranslationResult translationResult)
        {
            _logger.Info($"Removing {wordInTranslationEntry} from {translationEntry}...");
            translationEntry.Translations.Remove(wordInTranslationEntry);
            _messenger.Send(this, MessengerTokens.PriorityRemoveToken);
            if (!translationEntry.Translations.Any())
            {
                _logger.Info("Restoring default translations...");
                translationEntry.Translations = translationResult.GetDefaultWords();
                var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationEntry);
                foreach (var word in translationEntryViewModel.Translations)
                    _messenger.Send(word, MessengerTokens.PriorityAddToken);
            }
        }

        private void TogglePriority()
        {
            _logger.Info($"Changing priority for {this} to {!IsPriority}");

            //TODO: Store priority in separate table - remove correlation ID thus it could be changed when reloading word - use Text just like in json
            //TODO: on Details or TrEntry reload - check this new table and apply priority - propagate it to every view via Events
            IsPriority = !IsPriority;
            var translationInfo = WordsProcessor.ReloadTranslationDetailsIfNeeded(_translationEntryRepository.GetById(TranslationEntryId));
            UpdateTranslationEntry(translationInfo);
            UpdateTranslationDetails(translationInfo);
        }

        private void UpdateCorrelatedPriority([NotNull] PriorityWord correlatedWord)
        {
            _logger.Info($"Updating priority in correlated word {correlatedWord}...");
            correlatedWord.IsPriority = IsPriority;
            _messenger.Send(this, MessengerTokens.PriorityChangeToken);
        }

        private void UpdateTranslationDetails([NotNull] TranslationInfo translationInfo)
        {
            _logger.Trace($"Changing priority for {this} in TranslationDetails...");

            var wordInTranslationDetails = GetWordInTranslationDetails(translationInfo.TranslationDetails);
            if (wordInTranslationDetails == null)
            {
                _logger.Warn($"No correlated word was found in TranslationDetails for {this}...");
                return;
            }

            UpdateCorrelatedPriority(wordInTranslationDetails);
            _translationDetailsRepository.Save(translationInfo.TranslationDetails);
            _logger.Trace($"TranslationDetails for {this} have been updated");
        }

        private void UpdateTranslationEntry([NotNull] TranslationInfo translationInfo)
        {
            _logger.Trace($"Changing priority for {this} in TranslationEntry...");
            //TODO: check already deleted / use transactions
            var wordInTranslationEntry = GetWordInTranslationEntry(translationInfo.TranslationEntry);
            if (wordInTranslationEntry == null)
            {
                if (IsPriority)
                    Add(translationInfo.TranslationEntry);
            }
            else
            {
                if (!IsPriority)
                    Remove(wordInTranslationEntry, translationInfo.TranslationEntry, translationInfo.TranslationDetails.TranslationResult);
                else
                    UpdateCorrelatedPriority(wordInTranslationEntry);
            }
            _translationEntryRepository.Save(translationInfo.TranslationEntry);
            _logger.Trace($"TranslationEntry for {this} has been updated");
        }
    }
}