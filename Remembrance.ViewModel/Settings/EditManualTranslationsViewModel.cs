using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Model;
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
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public EditManualTranslationsViewModel([NotNull] ILog logger, [NotNull] IWordsProcessor wordsProcessor, [NotNull] IMessageHub messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            EditManualTranslationsCommand = new CorrelationCommand<TranslationEntryViewModel>(EditManualTranslations);
            DeleteCommand = new CorrelationCommand<ManualTranslation>(DeleteAsync);
            CancelCommand = new CorrelationCommand(Cancel);
            AddTranslationCommand = new CorrelationCommand(AddTranslation);
            SaveCommand = new CorrelationCommand(SaveAsync);
        }

        [NotNull]
        public static PartOfSpeech[] AvailablePartsOfSpeech { get; } = Enum.GetValues(typeof(PartOfSpeech))
            .Cast<PartOfSpeech>()
            .ToArray();

        private TranslationEntryViewModel TranslationEntryViewModel { get; set; }

        public bool IsManualTranslationsDialogOpen { get; private set; }

        [CanBeNull]
        public string ManualTranslationText { get; set; }

        [NotNull]
        public ICommand EditManualTranslationsCommand { get; }

        [NotNull]
        public ICommand DeleteCommand { get; }

        [NotNull]
        public ICommand CancelCommand { get; }

        [NotNull]
        public ICommand AddTranslationCommand { get; }

        [NotNull]
        public ICommand SaveCommand { get; }

        [NotNull]
        public ObservableCollection<ManualTranslation> ManualTranslations { get; private set; }

        private void AddTranslation()
        {
            if (string.IsNullOrWhiteSpace(ManualTranslationText))
                throw new LocalizableException(Errors.WordIsMissing);

            var newManualTranslation = new ManualTranslation(ManualTranslationText);
            _logger.Trace($"Adding {ManualTranslationText}...");
            ManualTranslationText = null;
            if (ManualTranslations.Any(x => x.Text.Equals(newManualTranslation.Text, StringComparison.InvariantCultureIgnoreCase)))
                throw new LocalizableException(Errors.TranslationIsPresent);

            ManualTranslations.Add(newManualTranslation);
        }

        private void Cancel()
        {
            _logger.Trace("Cancelling...");
            IsManualTranslationsDialogOpen = false;
        }

        private async void DeleteAsync([NotNull] ManualTranslation manualTranslation)
        {
            if (manualTranslation == null)
                throw new ArgumentNullException(nameof(manualTranslation));

            _logger.Trace($"Deleting manual translation {manualTranslation}...");
            if (ManualTranslations.Count == 1)
            {
                var translationDetails = await _wordsProcessor.ReloadTranslationDetailsIfNeededAsync(
                        TranslationEntryViewModel.Id,
                        TranslationEntryViewModel.Text,
                        TranslationEntryViewModel.Language,
                        TranslationEntryViewModel.TargetLanguage,
                        TranslationEntryViewModel.ManualTranslations,
                        CancellationToken.None)
                    .ConfigureAwait(false);
                //if non manual translations not exist
                if (translationDetails.TranslationResult.PartOfSpeechTranslations.All(t => t.IsManual))
                    throw new LocalizableException(Errors.CannotDeleteManual);
            }

            ManualTranslations.Remove(manualTranslation);
        }

        private void EditManualTranslations([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            TranslationEntryViewModel = translationEntryViewModel ?? throw new ArgumentNullException(nameof(translationEntryViewModel));
            ManualTranslations = translationEntryViewModel.ManualTranslations != null
                ? new ObservableCollection<ManualTranslation>(translationEntryViewModel.ManualTranslations)
                : new ObservableCollection<ManualTranslation>();
            _logger.Trace($"Editing manual translation for {translationEntryViewModel}...");
            IsManualTranslationsDialogOpen = true;
            ManualTranslationText = null;
        }

        private async void SaveAsync()
        {
            _logger.Trace("Saving...");
            TranslationEntryViewModel.ManualTranslations = ManualTranslations.Any()
                ? ManualTranslations.ToArray()
                : null;
            IsManualTranslationsDialogOpen = false;
            var translationInfo = await _wordsProcessor.UpdateManualTranslationsAsync(TranslationEntryViewModel.Id, TranslationEntryViewModel.ManualTranslations, CancellationToken.None)
                .ConfigureAwait(false);
            _messenger.Publish(translationInfo);
        }
    }
}