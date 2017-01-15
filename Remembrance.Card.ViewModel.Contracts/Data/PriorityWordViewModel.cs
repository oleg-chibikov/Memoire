using System;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Data.WordsTranslator;
using Remembrance.Translate.Contracts.Interfaces;
using Remembrance.TypeAdapter.Contracts;

namespace Remembrance.Card.ViewModel.Contracts.Data
{
    public class PriorityWordViewModel : WordViewModel
    {
        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly IMessenger messenger;

        [NotNull]
        private readonly ITranslationDetailsRepository translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository translationEntryRepository;

        [NotNull]
        private readonly IViewModelAdapter viewModelAdapter;

        private bool isPriority;

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
            if (translationEntryRepository == null)
                throw new ArgumentNullException(nameof(translationEntryRepository));
            if (translationDetailsRepository == null)
                throw new ArgumentNullException(nameof(translationDetailsRepository));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            this.translationEntryRepository = translationEntryRepository;
            this.translationDetailsRepository = translationDetailsRepository;
            this.viewModelAdapter = viewModelAdapter;
            this.messenger = messenger;
            this.logger = logger;
            TogglePriorityCommand = new RelayCommand(TogglePriority);
        }

        public Guid CorrelationId { get; set; }

        public ICommand TogglePriorityCommand { get; }

        public override bool CanTogglePriority { get; } = true;

        public bool IsPriority
        {
            get { return isPriority; }
            set { Set(() => IsPriority, ref isPriority, value); }
        }

        [CanBeNull]
        public TranslationEntryViewModel ParentTranslationEntry { get; set; }

        [CanBeNull]
        public TranslationDetailsViewModel ParentTranslationDetails { get; set; }

        private void TogglePriority()
        {
            logger.Info($"Changing priority for {this} to {!IsPriority}");

            IsPriority = !IsPriority;
            if (ParentTranslationDetails != null)
            {
                logger.Debug($"Changing priority for {this} in TranslationDetails...");
                var translationDetails = viewModelAdapter.Adapt<TranslationDetails>(ParentTranslationDetails);
                translationDetailsRepository.Save(translationDetails);

                //TODO: check already deleted
                var translationEntry = translationEntryRepository.GetById(ParentTranslationDetails.Id);
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
                translationEntryRepository.Save(translationEntry);
                logger.Debug($"TranslationEntry for {this} has been updated");
            }

            else if (ParentTranslationEntry != null)
            {
                logger.Debug($"Changing priority for {this} in TranslationEntry...");
                var translationEntry = viewModelAdapter.Adapt<TranslationEntry>(ParentTranslationEntry);
                var translationDetails = translationDetailsRepository.GetById(ParentTranslationEntry.Id);

                if (!IsPriority)
                {
                    var priorityWord = translationEntry.Translations.Single(x => x.CorrelationId == CorrelationId);
                    Remove(priorityWord, translationEntry, translationDetails);
                }

                translationEntryRepository.Save(translationEntry);
                logger.Debug($"Updating TranslationDetails for {this}...");

                var wordInTranslationDetails = translationDetails.GetWordInTranslationVariants(CorrelationId);
                if (wordInTranslationDetails == null)
                {
                    logger.Warn($"No correlated word was found in TranslationDetails for {this}...");
                    return;
                }
                UpdateCorrelatedPriority(wordInTranslationDetails);
                translationDetailsRepository.Save(translationDetails);
                logger.Debug($"TranslationDetails for {this} have been updated");
            }
        }

        private void UpdateCorrelatedPriority([NotNull] PriorityWord correlatedWord)
        {
            logger.Info($"Updating priority in correlated word {correlatedWord}...");
            correlatedWord.IsPriority = IsPriority;
            messenger.Send(this, MessengerTokens.PriorityChangeToken);
        }

        private void Add([NotNull] TranslationEntry translationEntry)
        {
            logger.Info($"Adding {this} to {translationEntry}...");
            var priorityWord = viewModelAdapter.Adapt<PriorityWord>(this);
            translationEntry.Translations.Add(priorityWord);
            messenger.Send(GetCurrentCopy(translationEntry), MessengerTokens.PriorityAddToken);
        }

        private void Remove([NotNull] PriorityWord wordInTranslationEntry, [NotNull] TranslationEntry translationEntry, [NotNull] TranslationDetails translationDetails)
        {
            logger.Info($"Removing {wordInTranslationEntry} from {translationEntry}...");
            translationEntry.Translations.Remove(wordInTranslationEntry);
            messenger.Send(this, MessengerTokens.PriorityRemoveToken);
            if (!translationEntry.Translations.Any())
            {
                logger.Info("Restoring default translations...");
                translationEntry.Translations = translationDetails.TranslationResult.GetDefaultWords();
                var translationEntryViewModel = viewModelAdapter.Adapt<TranslationEntryViewModel>(translationEntry);
                foreach (var word in translationEntryViewModel.Translations)
                    messenger.Send(word, MessengerTokens.PriorityAddToken);
            }
        }

        /// <summary>
        /// Get a copy of translationEntry in order to prevent links break in external handlers
        /// </summary>
        private PriorityWordViewModel GetCurrentCopy([NotNull] TranslationEntry translationEntry)
        {
            var translationEntryViewModel = viewModelAdapter.Adapt<TranslationEntryViewModel>(translationEntry);
            var word = translationEntryViewModel.Translations.Single(x => x.CorrelationId == CorrelationId);
            return word;
        }
    }
}