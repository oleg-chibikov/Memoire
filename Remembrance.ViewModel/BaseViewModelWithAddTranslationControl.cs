using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Languages.Data;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Common.View.Contracts;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseViewModelWithAddTranslationControl : BaseViewModel
    {
        readonly ILanguageManager _languageManager;
        readonly ILocalSettingsRepository _localSettingsRepository;
        string _selectedSourceLanguage;
        Language _selectedSourceLanguageItem;
        string _selectedTargetLanguage;
        Language _selectedTargetLanguageItem;

        protected BaseViewModelWithAddTranslationControl(
            ILocalSettingsRepository localSettingsRepository,
            ILanguageManager languageManager,
            ITranslationEntryProcessor translationEntryProcessor,
            ILog logger,
            ICommandManager commandManager) : base(commandManager)
        {
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            logger.Debug("Languages have been loaded");
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

        protected ILog Logger { get; }

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
            Logger.Info($"Adding translation for {translationEntryAdditionInfo}{addition}...");
            Text = null;
            ManualTranslation = null;

            var window = await GetWindowAsync().ConfigureAwait(false);
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(translationEntryAdditionInfo, CancellationTokenSource.Token, window, manualTranslations: manualTranslation)
                .ConfigureAwait(false);
        }
    }
}
