using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class EditManualTranslationsViewModel
    {
        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        public TranslationEntryKey TranslationEntryKey { get; private set; } = new TranslationEntryKey();

        public EditManualTranslationsViewModel(
            [NotNull] ILog logger,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IMessageHub messageHub,
            [NotNull] ITranslationEntryRepository translationEntryRepository)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EditManualTranslationsCommand = new CorrelationCommand<TranslationEntryViewModel>(EditManualTranslations);
            DeleteCommand = new AsyncCorrelationCommand<ManualTranslation>(DeleteAsync);
            CancelCommand = new CorrelationCommand(Cancel);
            AddTranslationCommand = new CorrelationCommand(AddTranslation);
            SaveCommand = new AsyncCorrelationCommand(SaveAsync);
        }

        [NotNull]
        public static ICollection<PartOfSpeech> AvailablePartsOfSpeech { get; } = Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>().ToArray();

        [NotNull]
        public ICommand AddTranslationCommand { get; }

        [NotNull]
        public ICommand CancelCommand { get; }

        [NotNull]
        public ICommand DeleteCommand { get; }

        [NotNull]
        public ICommand EditManualTranslationsCommand { get; }

        public bool IsManualTranslationsDialogOpen { get; private set; }

        [NotNull]
        public ObservableCollection<ManualTranslation> ManualTranslations { get; } = new ObservableCollection<ManualTranslation>();

        [CanBeNull]
        public string ManualTranslationText { get; set; }

        [NotNull]
        public ICommand SaveCommand { get; }

        private void AddTranslation()
        {
            if (string.IsNullOrWhiteSpace(ManualTranslationText))
            {
                throw new LocalizableException(Errors.WordIsMissing, "Word is not specified");
            }

            var newManualTranslation = new ManualTranslation(ManualTranslationText);
            _logger.TraceFormat("Adding {0}...", ManualTranslationText);
            ManualTranslationText = null;
            if (ManualTranslations.Any(x => x.Text.Equals(newManualTranslation.Text, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new LocalizableException(Errors.TranslationIsPresent, "Translations is already present in the dictionary");
            }

            ManualTranslations.Add(newManualTranslation);
        }

        private void Cancel()
        {
            _logger.Trace("Cancelling...");
            IsManualTranslationsDialogOpen = false;
        }

        [NotNull]
        private async Task DeleteAsync([NotNull] ManualTranslation manualTranslation)
        {
            if (manualTranslation == null)
            {
                throw new ArgumentNullException(nameof(manualTranslation));
            }

            _logger.TraceFormat("Deleting manual translation {0}...", manualTranslation);
            if (ManualTranslations.Count == 1)
            {
                var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(TranslationEntryKey, ManualTranslations, CancellationToken.None)
                    .ConfigureAwait(false);

                // if non manual translations not exist
                if (translationDetails.TranslationResult.PartOfSpeechTranslations.All(t => t.IsManual))
                {
                    throw new LocalizableException(Errors.CannotDeleteManual, "Cannot delete manual translation");
                }
            }

            ManualTranslations.Remove(manualTranslation);
        }

        private void EditManualTranslations([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            _logger.TraceFormat("Editing manual translation for {0}...", translationEntryViewModel);
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            TranslationEntryKey = translationEntryViewModel.Id;
            ManualTranslations.Clear();
            IsManualTranslationsDialogOpen = true;
            ManualTranslationText = null;
            var translationEntry = _translationEntryRepository.GetById(TranslationEntryKey);
            if (translationEntry.ManualTranslations == null)
            {
                return;
            }

            foreach (var manualTranslation in translationEntry.ManualTranslations)
            {
                ManualTranslations.Add(manualTranslation);
            }
        }

        [NotNull]
        private async Task SaveAsync()
        {
            _logger.Trace("Saving...");
            IsManualTranslationsDialogOpen = false;
            var translationInfo = await _translationEntryProcessor.UpdateManualTranslationsAsync(TranslationEntryKey, ManualTranslations, CancellationToken.None)
                .ConfigureAwait(false);
            _messageHub.Publish(translationInfo.TranslationEntry);
        }
    }
}