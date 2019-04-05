using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.Exceptions;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class EditManualTranslationsViewModel : BaseViewModel
    {
        private readonly ILog _logger;

        private readonly IMessageHub _messageHub;

        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        private readonly ITranslationEntryRepository _translationEntryRepository;

        public EditManualTranslationsViewModel(
            ILog logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messageHub,
            ITranslationEntryRepository translationEntryRepository,
            ICommandManager commandManager)
            : base(commandManager)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EditManualTranslationsCommand = AddCommand<TranslationEntryViewModel>(EditManualTranslations);
            DeleteCommand = AddCommand<ManualTranslation>(DeleteAsync);
            CancelCommand = AddCommand(Cancel);
            AddTranslationCommand = AddCommand(AddTranslation);
            SaveCommand = AddCommand(SaveAsync);
        }

        public static IReadOnlyCollection<PartOfSpeech> AvailablePartsOfSpeech { get; } = Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>().ToArray();

        public ICommand AddTranslationCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand EditManualTranslationsCommand { get; }

        public bool IsManualTranslationsDialogOpen { get; private set; }

        public ObservableCollection<ManualTranslation> ManualTranslations { get; } = new ObservableCollection<ManualTranslation>();

        public string? ManualTranslationText { get; set; }

        public ICommand SaveCommand { get; }

        public TranslationEntryKey TranslationEntryKey { get; private set; } = new TranslationEntryKey();

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

        private async Task DeleteAsync(ManualTranslation manualTranslation)
        {
            _ = manualTranslation ?? throw new ArgumentNullException(nameof(manualTranslation));
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

        private void EditManualTranslations(TranslationEntryViewModel translationEntryViewModel)
        {
            _logger.TraceFormat("Editing manual translation for {0}...", translationEntryViewModel);
            _ = translationEntryViewModel ?? throw new ArgumentNullException(nameof(translationEntryViewModel));
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