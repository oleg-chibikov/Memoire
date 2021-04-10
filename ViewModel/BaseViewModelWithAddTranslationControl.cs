using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Languages.Data;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Common.View.Contracts;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseViewModelWithAddTranslationControl : BaseViewModel
    {
        readonly ILanguageManager _languageManager;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ILogger _logger;
        string _selectedSourceLanguage;
        Language _selectedSourceLanguageItem;
        string _selectedTargetLanguage;
        Language _selectedTargetLanguageItem;

        protected BaseViewModelWithAddTranslationControl(
            ILocalSettingsRepository localSettingsRepository,
            ILanguageManager languageManager,
            ITranslationEntryProcessor translationEntryProcessor,
            ILogger<BaseViewModelWithAddTranslationControl> logger,
            ICommandManager commandManager) : base(commandManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace($"Initializing {GetType().Name}...");
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            SaveCommand = AddCommand(SaveAsync);

            var sourceLanguages = _languageManager.GetAvailableSourceLanguages();
            AvailableSourceLanguages = sourceLanguages;
            _selectedSourceLanguage = sourceLanguages.SelectedLanguage;
            _selectedSourceLanguageItem = sourceLanguages.SelectedLanguageItem;

            var targetLanguages = _languageManager.GetAvailableTargetLanguages(_selectedSourceLanguage);
            AvailableTargetLanguages = new ObservableCollection<Language>(targetLanguages);
            _selectedTargetLanguage = targetLanguages.SelectedLanguage;
            _selectedTargetLanguageItem = sourceLanguages.SelectedLanguageItem;
            logger.LogDebug("Languages have been loaded");
        }

        public IReadOnlyCollection<Language> AvailableSourceLanguages { get; }

        public ObservableCollection<Language> AvailableTargetLanguages { get; }

        public string? ManualTranslation { get; set; }

        public ICommand SaveCommand { get; }

        public Language SelectedSourceLanguageItem
        {
            get => _selectedSourceLanguageItem;
            set
            {
                _selectedSourceLanguageItem = value;
                SelectedSourceLanguage = value?.Code ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public Language SelectedTargetLanguageItem
        {
            get => _selectedTargetLanguageItem;
            set
            {
                _selectedTargetLanguageItem = value;
                SelectedTargetLanguage = value?.Code ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public string SelectedSourceLanguage
        {
            get => _selectedSourceLanguage;
            set
            {
                _selectedSourceLanguage = value;
                _localSettingsRepository.LastUsedSourceLanguage = value;
                var targetLanguages = _languageManager.GetAvailableTargetLanguages(value);
                AvailableTargetLanguages.Clear();
                foreach (var targetLanguage in targetLanguages)
                {
                    AvailableTargetLanguages.Add(targetLanguage);
                }

                SelectedTargetLanguage = targetLanguages.SelectedLanguage;
            }
        }

        public string SelectedTargetLanguage
        {
            get => _selectedTargetLanguage;
            set
            {
                _selectedTargetLanguage = value;

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (value != null)
                {
                    _localSettingsRepository.LastUsedTargetLanguage = value;
                }
            }
        }

        public string? Text { get; set; }

        protected CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        protected ITranslationEntryProcessor TranslationEntryProcessor { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancellationTokenSource.Cancel();
                Cleanup();
                CancellationTokenSource.Dispose();
            }

            base.Dispose(disposing);
        }

        protected virtual void Cleanup()
        {
        }

        protected abstract Task<IDisplayable?> GetWindowAsync();

        async Task SaveAsync()
        {
            var text = Text;
            var manualTranslation = (ManualTranslation == null) || string.IsNullOrWhiteSpace(ManualTranslation)
                ? null
                : new[]
                {
                    new ManualTranslation(ManualTranslation)
                };
            var translationEntryAdditionInfo = new TranslationEntryAdditionInfo(text, SelectedSourceLanguage, SelectedTargetLanguage);
            var addition = manualTranslation == null ? null : $" with manual translation {ManualTranslation}";
            _logger.LogInformation($"Adding translation for {translationEntryAdditionInfo}{addition}...");
            Text = null;
            ManualTranslation = null;

            var window = await GetWindowAsync().ConfigureAwait(false);
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(translationEntryAdditionInfo, window, manualTranslations: manualTranslation, cancellationToken: CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
    }
}
