using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Exceptions;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class EditManualTranslationsViewModel : BaseViewModel
    {
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly ITranslationEntryProcessor _translationEntryProcessor;
        readonly ITranslationEntryRepository _translationEntryRepository;

        public EditManualTranslationsViewModel(
            ILogger<EditManualTranslationsViewModel> logger,
            ITranslationEntryProcessor translationEntryProcessor,
            IMessageHub messageHub,
            ITranslationEntryRepository translationEntryRepository,
            ICommandManager commandManager) : base(commandManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {}...", GetType().Name);
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            EditManualTranslationsCommand = AddCommand<TranslationEntryViewModel>(EditManualTranslations);
            DeleteCommand = AddCommand<ManualTranslation>(DeleteAsync);
            CancelCommand = AddCommand(Cancel);
            AddTranslationCommand = AddCommand(AddTranslation);
            SaveCommand = AddCommand(SaveAsync);
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public static IEnumerable<PartOfSpeech> AvailablePartsOfSpeech { get; } = Enum.GetValues(typeof(PartOfSpeech)).Cast<PartOfSpeech>().ToArray();

        public ICommand AddTranslationCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand EditManualTranslationsCommand { get; }

        public bool IsManualTranslationsDialogOpen { get; private set; }

        public ObservableCollection<ManualTranslation> ManualTranslations { get; } = new ();

        public string? ManualTranslationText { get; set; }

        public ICommand SaveCommand { get; }

        public TranslationEntryKey TranslationEntryKey { get; private set; } = new ();

        void AddTranslation()
        {
            if ((ManualTranslationText == null) || string.IsNullOrWhiteSpace(ManualTranslationText))
            {
                throw new LocalizableException(Errors.WordIsMissing, "Word is not specified");
            }

            var newManualTranslation = new ManualTranslation(ManualTranslationText);
            _logger.LogTrace("Adding {ManualTranslation}...", ManualTranslationText);
            ManualTranslationText = null;
            if (ManualTranslations.Any(x => x.Text.Equals(newManualTranslation.Text, StringComparison.OrdinalIgnoreCase)))
            {
                throw new LocalizableException(Errors.TranslationIsPresent, "Translations is already present in the dictionary");
            }

            ManualTranslations.Add(newManualTranslation);
        }

        void Cancel()
        {
            _logger.LogTrace("Cancelling...");
            IsManualTranslationsDialogOpen = false;
        }

        async Task DeleteAsync(ManualTranslation manualTranslation)
        {
            _ = manualTranslation ?? throw new ArgumentNullException(nameof(manualTranslation));
            _logger.LogTrace("Deleting manual translation {ManualTranslation}...", manualTranslation);
            if (ManualTranslations.Count == 1)
            {
                var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(TranslationEntryKey, ManualTranslations, CancellationToken.None).ConfigureAwait(false);

                // if non manual translations not exist
                if (translationDetails.TranslationResult.PartOfSpeechTranslations.All(t => t.IsManual))
                {
                    throw new LocalizableException(Errors.CannotDeleteManual, "Cannot delete manual translation");
                }
            }

            ManualTranslations.Remove(manualTranslation);
        }

        void EditManualTranslations(TranslationEntryViewModel translationEntryViewModel)
        {
            _logger.LogTrace("Editing manual translation for {Translation}...", translationEntryViewModel);
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

        async Task SaveAsync()
        {
            _logger.LogTrace("Saving...");
            IsManualTranslationsDialogOpen = false;
            var translationInfo = await _translationEntryProcessor.UpdateManualTranslationsAsync(TranslationEntryKey, ManualTranslations, CancellationToken.None).ConfigureAwait(false);
            _messageHub.Publish(translationInfo.TranslationEntry);
        }
    }
}
