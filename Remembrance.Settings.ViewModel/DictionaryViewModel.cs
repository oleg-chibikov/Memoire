using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using Common.Logging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;
using Remembrance.Card.ViewModel.Contracts.Data;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Settings.View.Contracts;
using Remembrance.Settings.ViewModel.Contracts;
using Remembrance.Settings.ViewModel.Contracts.Data;
using Remembrance.Translate.Contracts.Interfaces;
using Remembrance.TypeAdapter.Contracts;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View;

// ReSharper disable InconsistentlySynchronizedField - logger and viewModelAdapter could be used inside or outside of lock

namespace Remembrance.Settings.ViewModel
{
    [UsedImplicitly]
    public sealed class DictionaryViewModel : ViewModelBase, IDictionaryViewModel, IDisposable
    {
        private readonly object _translationListLock = new object();

        public DictionaryViewModel(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessenger messenger)
        {
            if (languageDetector == null)
                throw new ArgumentNullException(nameof(languageDetector));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));

            SaveCommand = new CorrelationCommand<string>(Save);
            DeleteCommand = new CorrelationCommand<TranslationEntryViewModel>(Delete);
            OpenDetailsCommand = new CorrelationCommand<TranslationEntryViewModel>(OpenDetails);
            OpenSettingsCommand = new CorrelationCommand(OpenSettings);
            SearchCommand = new CorrelationCommand<string>(Search);

            logger.Info("Starting...");

            messenger.Register<TranslationInfo>(this, MessengerTokens.TranslationInfoToken, OnWordReceived);
            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, OnUiLanguageChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityChangeToken, OnPriorityChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityAddToken, OnPriorityAdded);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityRemoveToken, OnPriorityRemoved);

            var settings = settingsRepository.Get();

            logger.Debug("Loading languages...");
            var languages = languageDetector.ListLanguagesAsync(CultureUtilities.GetCurrentCulture().TwoLetterISOLanguageName).Result;

            var availableLanguages = languages.Languages.Select(x => new Language(x.Key, x.Value)).OrderBy(x => x.DisplayName);

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

            Language targetLanguage = null;
            if (settings.LastUsedTargetLanguage != null)
                targetLanguage = AvailableTargetLanguages.SingleOrDefault(x => x.Code == settings.LastUsedTargetLanguage);
            if (targetLanguage == null)
                targetLanguage = autoTargetLanguage;
            _selectedTargetLanguage = targetLanguage;

            Language sourceLanguage = null;
            if (settings.LastUsedSourceLanguage != null)
                sourceLanguage = AvailableSourceLanguages.SingleOrDefault(x => x.Code == settings.LastUsedSourceLanguage);
            if (sourceLanguage == null)
                sourceLanguage = autoSourceLanguage;
            _selectedSourceLanguage = sourceLanguage;
            logger.Debug("Languages has been loaded");

            //var acceptableLanguages = languages.Directions.Where(x => x.StartsWith(UiLanguage)).Select(x => x.Split('-')[1]).Concat(new []{UiLanguage}).ToArray();
            //AvailableTargetLanguages = languages.Languages.Where(x => acceptableLanguages.Contains(x.Key)).Select(x => new Language(x.Key, x.Value)).ToArray();

            logger.Debug("Receiving translations...");
            var translationEntryViewModels = viewModelAdapter.Adapt<TranslationEntryViewModel[]>(translationEntryRepository.GetAll());
            foreach (var translationEntryViewModel in translationEntryViewModels)
                translationEntryViewModel.TextChanged += TranslationEntryViewModel_TextChanged;

            TranslationList = new ObservableCollection<TranslationEntryViewModel>(translationEntryViewModels);
            logger.Debug("Translations has been received");

            TranslationList.CollectionChanged += TranslationList_CollectionChanged;

            View = CollectionViewSource.GetDefaultView(TranslationList);
            // uncomment to use observable collection from another thread
            // BindingOperations.EnableCollectionSynchronization(TranslationList, lockObject);

            logger.Debug("Creating NextCardShowTime update timer...");
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;

            logger.Info("Started");
        }

        [NotNull]
        private ObservableCollection<TranslationEntryViewModel> TranslationList { get; }

        public ICollectionView View { get; }

        public void Dispose()
        {
            _logger.Debug("Disposing...");
            TranslationList.CollectionChanged -= TranslationList_CollectionChanged;
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
            _logger.Debug("Disposed");
        }

        #region Dependencies

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        [NotNull]
        private readonly DispatcherTimer _timer;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        #endregion

        #region EventHandlers

        private void OnWordReceived([NotNull] TranslationInfo translationInfo)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));

            _logger.Debug($"Received {translationInfo} from external source...");
            var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationInfo.TranslationEntry);

            lock (_translationListLock)
            {
                var existing = TranslationList.SingleOrDefault(x => x.Id == translationInfo.TranslationEntry.Id);
                if (existing != null)
                {
                    _logger.Debug($"Updating {existing} in the list...");
                    //Prevent text change to fire
                    using (existing.SupressNotification())
                    {
                        _viewModelAdapter.Adapt(translationInfo.TranslationEntry, existing);
                    }
                    Application.Current.Dispatcher.InvokeAsync(() => View.MoveCurrentTo(existing));
                    _logger.Debug($"{existing} has been updated in the list");
                }
                else
                {
                    _logger.Debug($"Adding {translationEntryViewModel} to the list...");
                    Application.Current.Dispatcher.InvokeAsync(
                        () =>
                        {
                            TranslationList.Add(translationEntryViewModel);
                            View.MoveCurrentToLast();
                        });
                    translationEntryViewModel.TextChanged += TranslationEntryViewModel_TextChanged;
                    _logger.Debug($"{translationEntryViewModel} has been added to the list...");
                }
            }
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            _logger.Debug($"Changing priority for {priorityWordViewModel} int the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.ParentTranslationEntry?.Id ?? priorityWordViewModel.ParentTranslationDetails?.Id;
            var changed = false;
            lock (_translationListLock)
            {
                var translationEntryViewModel = TranslationList.SingleOrDefault(x => x.Id == parentId);
                var translation = translationEntryViewModel?.Translations.SingleOrDefault(x => x.CorrelationId == priorityWordViewModel.CorrelationId);
                if (translation != null)
                {
                    translation.IsPriority = priorityWordViewModel.IsPriority;
                    changed = true;
                }
            }
            if (changed)
                _logger.Debug($"Changed priority for {priorityWordViewModel}");
            else
                _logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnPriorityAdded([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            _logger.Debug($"Adding {priorityWordViewModel} to the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.ParentTranslationDetails?.Id ?? priorityWordViewModel.ParentTranslationEntry?.Id;
            TranslationEntryViewModel translationEntryViewModel;
            lock (_translationListLock)
            {
                translationEntryViewModel = TranslationList.SingleOrDefault(x => x.Id == parentId);
                if (translationEntryViewModel != null)
                {
                    priorityWordViewModel.ParentTranslationDetails = null;
                    priorityWordViewModel.ParentTranslationEntry = translationEntryViewModel;
                    translationEntryViewModel.Translations.Add(priorityWordViewModel);
                }
            }
            if (translationEntryViewModel != null)
                _logger.Debug($"Added {priorityWordViewModel} to {translationEntryViewModel}");
            else
                _logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnPriorityRemoved([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            _logger.Debug($"Removing {priorityWordViewModel} from the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.ParentTranslationDetails?.Id ?? priorityWordViewModel.ParentTranslationEntry?.Id;
            var removed = false;
            lock (_translationListLock)
            {
                var translationEntryViewModel = TranslationList.SingleOrDefault(x => x.Id == parentId);
                var correlated = translationEntryViewModel?.Translations.SingleOrDefault(x => x.CorrelationId == priorityWordViewModel.CorrelationId);
                if (correlated != null)
                    removed = translationEntryViewModel.Translations.Remove(correlated);
            }
            if (removed)
                _logger.Debug($"Removed {priorityWordViewModel} from the list");
            else
                _logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnUiLanguageChanged([NotNull] string uiLanguage)
        {
            _logger.Debug($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage);
            // ReSharper disable once ExplicitCallerInfoArgument
            foreach (var translation in TranslationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                translation.RaisePropertyChanged(nameof(translation.PartOfSpeech));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var translation in TranslationList)
            {
                var time = translation.NextCardShowTime;
                translation.NextCardShowTime = time.AddTicks(1); //To launch converter
            }
        }

        private bool TranslationEntryViewModel_TextChanged([NotNull] object sender, [NotNull] TextChangedEventArgs e)
        {
            var translationEntryViewModel = (TranslationEntryViewModel) sender;
            _logger.Info($"Changing translation's text for {translationEntryViewModel} to {e.NewValue}...");

            var sourceLanguage = translationEntryViewModel.Language;
            var targetLanguage = translationEntryViewModel.TargetLanguage;
            return e.NewValue != null && _wordsProcessor.ChangeText(translationEntryViewModel.Id, e.NewValue, sourceLanguage, targetLanguage);
        }

        private void TranslationList_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Remove)
                return;

            foreach (TranslationEntryViewModel translationEntryViewModel in e.OldItems)
            {
                _translationDetailsRepository.Delete(translationEntryViewModel.Id);
                _translationEntryRepository.Delete(translationEntryViewModel.Id);
            }
        }

        #endregion

        #region Dependency properties

        public Language[] AvailableTargetLanguages { get; }

        public Language[] AvailableSourceLanguages { get; }

        private Language _selectedTargetLanguage;

        public Language SelectedTargetLanguage
        {
            get { return _selectedTargetLanguage; }
            [UsedImplicitly]
            set
            {
                Set(() => SelectedTargetLanguage, ref _selectedTargetLanguage, value);
                var settings = _settingsRepository.Get();
                settings.LastUsedTargetLanguage = value.Code;
                _settingsRepository.Save(settings);
            }
        }

        private Language _selectedSourceLanguage;

        public Language SelectedSourceLanguage
        {
            get { return _selectedSourceLanguage; }
            [UsedImplicitly]
            set
            {
                Set(() => SelectedSourceLanguage, ref _selectedSourceLanguage, value);
                var settings = _settingsRepository.Get();
                settings.LastUsedSourceLanguage = value.Code;
                _settingsRepository.Save(settings);
            }
        }

        private string _newItemSource;

        public string NewItemSource
        {
            get { return _newItemSource; }
            [UsedImplicitly]
            set { Set(() => NewItemSource, ref _newItemSource, value); }
        }

        private string _searchText;

        public string SearchText
        {
            get { return _searchText; }
            [UsedImplicitly]
            set { Set(() => SearchText, ref _searchText, value); }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand OpenDetailsCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand SearchCommand { get; }

        #endregion

        #region Command handlers

        private void Save([NotNull] string text)
        {
            _logger.Info($"Adding translation for {text}...");
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            NewItemSource = null;

            var sourceLanguage = SelectedSourceLanguage.Code;
            var targetLanguage = SelectedTargetLanguage.Code;

            _wordsProcessor.ProcessNewWord(text, sourceLanguage, targetLanguage);
        }

        private void Delete([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            _logger.Debug($"Deleting {translationEntryViewModel} from the list...");
            if (translationEntryViewModel == null)
                throw new ArgumentNullException(nameof(translationEntryViewModel));

            bool deleted;
            lock (_translationListLock)
            {
                deleted = TranslationList.Remove(translationEntryViewModel);
            }
            translationEntryViewModel.TextChanged -= TranslationEntryViewModel_TextChanged;
            if (!deleted)
                _logger.Warn($"{translationEntryViewModel} is not deleted from the list");
            else
                _logger.Debug($"{translationEntryViewModel} has been deleted from the list");
        }

        private void OpenDetails([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            _logger.Debug($"Opening details for {translationEntryViewModel}...");
            if (translationEntryViewModel == null)
                throw new ArgumentNullException(nameof(translationEntryViewModel));

            var translationDetails = _translationDetailsRepository.GetById(translationEntryViewModel.Id);
            var translationEntry = _viewModelAdapter.Adapt<TranslationEntry>(translationEntryViewModel);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
            var translationResultCardViewModel = _lifetimeScope.Resolve<ITranslationResultCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            var detailsWindow = _lifetimeScope.Resolve<ITranslationResultCardWindow>(
                new TypedParameter(typeof(Window), dictionaryWindow),
                new TypedParameter(typeof(ITranslationResultCardViewModel), translationResultCardViewModel));
            detailsWindow.Show();
        }

        private void OpenSettings()
        {
            _logger.Debug("Opening settings...");
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            _lifetimeScope.Resolve<WindowFactory<ISettingsWindow>>().GetOrCreateWindow(dictionaryWindowParameter).Restore();
        }

        private void Search([CanBeNull] string text)
        {
            _logger.Debug($"Searching for {text}...");
            View.Filter = o => string.IsNullOrWhiteSpace(text) || ((TranslationEntryViewModel) o).Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        #endregion
    }
}