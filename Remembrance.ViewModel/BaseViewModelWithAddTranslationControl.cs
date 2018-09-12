using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Languages;
using Remembrance.Contracts.Languages.Data;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseViewModelWithAddTranslationControl : IDisposable
    {
        [NotNull]
        protected readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly ITranslationEntryProcessor TranslationEntryProcessor;

        [NotNull]
        private readonly ILanguageManager _languageManager;

        [NotNull]
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private string _selectedSourceLanguage;

        [NotNull]
        private string _selectedTargetLanguage;

        protected BaseViewModelWithAddTranslationControl(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILanguageManager languageManager,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger)
        {
            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            SaveCommand = new AsyncCorrelationCommand(SaveAsync);

            var sourceLanguages = _languageManager.GetAvailableSourceLanguages();
            AvailableSourceLanguages = sourceLanguages;
            _selectedSourceLanguage = sourceLanguages.SelectedLanguage;

            var targetLanguages = _languageManager.GetAvailableTargetLanguages(_selectedSourceLanguage);
            AvailableTargetLanguages = new ObservableCollection<Language>(targetLanguages);
            _selectedTargetLanguage = targetLanguages.SelectedLanguage;
            logger.Debug("Languages have been loaded");
        }

        [NotNull]
        public IReadOnlyCollection<Language> AvailableSourceLanguages { get; }

        [NotNull]
        public ObservableCollection<Language> AvailableTargetLanguages { get; }

        [CanBeNull]
        public string ManualTranslation { get; set; }

        [NotNull]
        public ICommand SaveCommand { get; }

        [NotNull]
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

        [NotNull]
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

        [CanBeNull]
        public string Text { get; set; }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            Cleanup();
        }

        protected virtual void Cleanup()
        {
        }

        [ItemCanBeNull]
        [NotNull]
        protected abstract Task<IWindow> GetWindowAsync();

        [NotNull]
        private async Task SaveAsync()
        {
            var text = Text;
            var manualTranslation = string.IsNullOrWhiteSpace(ManualTranslation)
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
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(
                    translationEntryAdditionInfo,
                    CancellationTokenSource.Token,
                    window,
                    manualTranslations: manualTranslation)
                .ConfigureAwait(false);
        }
    }
}