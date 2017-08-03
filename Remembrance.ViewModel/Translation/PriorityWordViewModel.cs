using System;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Contracts.TypeAdapter;
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
        private readonly IViewModelAdapter _viewModelAdapter;

        public PriorityWordViewModel(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessenger messenger,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger)
            : base(textToSpeechPlayer, wordsProcessor)
        {
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            TogglePriorityCommand = new CorrelationCommand(TogglePriority);
        }

        [DoNotNotify]
        public Guid CorrelationId
        {
            get;
            [UsedImplicitly]
            set;
        }

        #region Commands

        public ICommand TogglePriorityCommand { get; }

        #endregion

        public override bool CanEdit { get; } = true;

        public bool IsPriority { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public TranslationEntryViewModel ParentTranslationEntryViewModel { get; set; }

        [CanBeNull]
        [DoNotNotify]
        public TranslationDetailsViewModel ParentTranslationDetailsViewModel { get; set; }

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
            var word = translationEntryViewModel.Translations.Single(x => x.CorrelationId == CorrelationId);
            return word;
        }

        private void Remove([NotNull] PriorityWord wordInTranslationEntry, [NotNull] TranslationEntry translationEntry, [NotNull] TranslationDetails translationDetails)
        {
            _logger.Info($"Removing {wordInTranslationEntry} from {translationEntry}...");
            translationEntry.Translations.Remove(wordInTranslationEntry);
            _messenger.Send(this, MessengerTokens.PriorityRemoveToken);
            if (!translationEntry.Translations.Any())
            {
                _logger.Info("Restoring default translations...");
                translationEntry.Translations = translationDetails.TranslationResult.GetDefaultWords();
                var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationEntry);
                foreach (var word in translationEntryViewModel.Translations)
                    _messenger.Send(word, MessengerTokens.PriorityAddToken);
            }
        }

        private void TogglePriority()
        {
            _logger.Info($"Changing priority for {this} to {!IsPriority}");

            IsPriority = !IsPriority;
            if (ParentTranslationDetailsViewModel != null)
            {
                _logger.Trace($"Changing priority for {this} in TranslationDetails...");
                var translationDetails = _viewModelAdapter.Adapt<TranslationDetails>(ParentTranslationDetailsViewModel);
                //Save parent details because it contains the current word
                _translationDetailsRepository.Save(translationDetails);

                //TODO: check already deleted
                var translationEntry = _translationEntryRepository.GetById(ParentTranslationDetailsViewModel.TranslationEntryId);
                var wordInTranslationEntry = translationEntry.Translations.SingleOrDefault(x => x.CorrelationId == CorrelationId);
                if (wordInTranslationEntry == null)
                {
                    if (IsPriority)
                        Add(translationEntry);
                }
                else
                {
                    if (!IsPriority)
                        Remove(wordInTranslationEntry, translationEntry, translationDetails);
                    else
                        UpdateCorrelatedPriority(wordInTranslationEntry);
                }
                _translationEntryRepository.Save(translationEntry);
                _logger.Trace($"TranslationEntry for {this} has been updated");
            }

            else if (ParentTranslationEntryViewModel != null)
            {
                _logger.Trace($"Changing priority for {this} in TranslationEntry...");
                var translationInfo = WordsProcessor.ReloadTranslationDetailsIfNeeded(_viewModelAdapter.Adapt<TranslationEntry>(ParentTranslationEntryViewModel));

                if (!IsPriority)
                {
                    var priorityWord = translationInfo.TranslationEntry.Translations.Single(x => x.CorrelationId == CorrelationId);
                    Remove(priorityWord, translationInfo.TranslationEntry, translationInfo.TranslationDetails);
                }

                _translationEntryRepository.Save(translationInfo.TranslationEntry);
                _logger.Trace($"Updating TranslationDetails for {this}...");

                var wordInTranslationDetails = translationInfo.TranslationDetails.GetWordInTranslationVariants(CorrelationId);
                if (wordInTranslationDetails == null)
                {
                    _logger.Warn($"No correlated word was found in TranslationDetails for {this}...");
                    return;
                }

                UpdateCorrelatedPriority(wordInTranslationDetails);
                _translationDetailsRepository.Save(translationInfo.TranslationDetails);
                _logger.Trace($"TranslationDetails for {this} have been updated");
            }
        }

        private void UpdateCorrelatedPriority([NotNull] PriorityWord correlatedWord)
        {
            _logger.Info($"Updating priority in correlated word {correlatedWord}...");
            correlatedWord.IsPriority = IsPriority;
            _messenger.Send(this, MessengerTokens.PriorityChangeToken);
        }
    }
}