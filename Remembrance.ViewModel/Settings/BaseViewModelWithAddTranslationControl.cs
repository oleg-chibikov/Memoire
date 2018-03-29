using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate;
using Remembrance.Resources;
using Remembrance.ViewModel.Settings.Data;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.ViewModel.Settings
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
        private readonly ILocalSettingsRepository _localSettingsRepository;

        [NotNull]
        private Language _selectedSourceLanguage;

        [NotNull]
        private Language _selectedTargetLanguage;

        protected BaseViewModelWithAddTranslationControl(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger)
        {
            if (languageDetector == null)
            {
                throw new ArgumentNullException(nameof(languageDetector));
            }

            TranslationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            SaveCommand = new AsyncCorrelationCommand(SaveAsync);

            logger.Trace("Loading languages...");
            var languages = languageDetector.ListLanguagesAsync(CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName, CancellationTokenSource.Token).Result;

            // var acceptableLanguages = languages.Directions.Where(x => x.StartsWith(UiLanguage)).Select(x => x.Split('-')[1]).Concat(new []{UiLanguage}).ToArray();
            // AvailableTargetLanguages = languages.Languages.Where(x => acceptableLanguages.Contains(x.Key)).Select(x => new Language(x.Key, x.Value)).ToArray();
            var availableLanguages = languages.Languages.Select(x => new Language(x.Key, x.Value)).OrderBy(x => x.DisplayName).ToArray();

            var autoSourceLanguage = new Language(Constants.AutoDetectLanguage, "--AutoDetect--");
            var autoTargetLanguage = new Language(Constants.AutoDetectLanguage, "--Reverse--");

            AvailableTargetLanguages = new[]
                {
                    autoTargetLanguage
                }.Concat(availableLanguages)
                .ToArray();
            AvailableSourceLanguages = new[]
                {
                    autoSourceLanguage
                }.Concat(availableLanguages)
                .ToArray();
            var lastUsedTargetLanguage = localSettingsRepository.LastUsedTargetLanguage;
            Language targetLanguage = null;
            if (lastUsedTargetLanguage != null)
            {
                targetLanguage = AvailableTargetLanguages.SingleOrDefault(x => x.Code == lastUsedTargetLanguage);
            }

            if (targetLanguage == null)
            {
                targetLanguage = autoTargetLanguage;
            }

            _selectedTargetLanguage = targetLanguage;

            Language sourceLanguage = null;
            var lastUsedSourceLanguage = localSettingsRepository.LastUsedSourceLanguage;
            if (lastUsedSourceLanguage != null)
            {
                sourceLanguage = AvailableSourceLanguages.SingleOrDefault(x => x.Code == lastUsedSourceLanguage);
            }

            if (sourceLanguage == null)
            {
                sourceLanguage = autoSourceLanguage;
            }

            _selectedSourceLanguage = sourceLanguage;
            logger.Debug("Languages have been loaded");
        }

        [NotNull]
        public ICollection<Language> AvailableSourceLanguages { get; }

        [NotNull]
        public ICollection<Language> AvailableTargetLanguages { get; }

        [CanBeNull]
        public string ManualTranslation { get; set; }

        [NotNull]
        public ICommand SaveCommand { get; }

        [NotNull]
        public Language SelectedSourceLanguage
        {
            get => _selectedSourceLanguage;
            set
            {
                _selectedSourceLanguage = value;
                _localSettingsRepository.LastUsedSourceLanguage = value.Code;
            }
        }

        [NotNull]
        public Language SelectedTargetLanguage
        {
            get => _selectedTargetLanguage;
            set
            {
                _selectedTargetLanguage = value;
                _localSettingsRepository.LastUsedTargetLanguage = value.Code;
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
            var translationEntryAdditionInfo = new TranslationEntryAdditionInfo(text, SelectedSourceLanguage.Code, SelectedTargetLanguage.Code);
            var addition = manualTranslation == null ? null : $" with manual translation {ManualTranslation}";
            Logger.Info($"Adding translation for {translationEntryAdditionInfo}{addition}...");
            Text = null;
            ManualTranslation = null;

            var window = await GetWindowAsync().ConfigureAwait(false);
            await TranslationEntryProcessor.AddOrUpdateTranslationEntryAsync(translationEntryAdditionInfo, CancellationTokenSource.Token, window, manualTranslations: manualTranslation).ConfigureAwait(false);
        }
    }
}