using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using CCSWE.Collections.ObjectModel;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.View.Card;
using Remembrance.Contracts.View.Settings;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Settings.Data;
using Remembrance.ViewModel.Translation;
using Scar.Common.DAL;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class DictionaryViewModel : BaseViewModelWithAddTranslationControl
    {
        //TODO: config
        private const int PageSize = 20;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        [NotNull]
        private readonly DispatcherTimer _timer;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly SynchronizedObservableCollection<TranslationEntryViewModel> _translationList;

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public DictionaryViewModel(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger)
            : base(settingsRepository, languageDetector, wordsProcessor, logger)
        {
            _messenger = messenger;
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));

            DeleteCommand = new CorrelationCommand<TranslationEntryViewModel>(Delete);
            OpenDetailsCommand = new CorrelationCommand<TranslationEntryViewModel>(OpenDetails);
            OpenSettingsCommand = new CorrelationCommand(OpenSettings);
            SearchCommand = new CorrelationCommand<string>(Search);

            Logger.Info("Starting...");

            _translationList = new SynchronizedObservableCollection<TranslationEntryViewModel>();

            _translationList.CollectionChanged += TranslationList_CollectionChanged;

            View = CollectionViewSource.GetDefaultView(_translationList);

            // uncomment to use observable collection from another thread
            // BindingOperations.EnableCollectionSynchronization(TranslationList, lockObject);
            Logger.Trace("Creating NextCardShowTime update timer...");
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;

            Logger.Trace("Subscribing to the events...");

            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo>(OnWordReceived));
            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo[]>(OnWordsBatchReceived));
            _subscriptionTokens.Add(messenger.Subscribe<Language>(OnUiLanguageChanged));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordViewModel>(OnPriorityChanged));

            Logger.Info("Started");
            Logger.Trace("Receiving translations...");
            LoadTranslationsAsync();
        }

        protected override IWindow Window => _dictionaryWindowFactory.GetWindowIfExists();

        [NotNull]
        public ICollectionView View { get; }

        public int Count { get; private set; }

        [CanBeNull]
        public string SearchText { get; set; }

        [NotNull]
        public ICommand DeleteCommand { get; }

        [NotNull]
        public ICommand OpenDetailsCommand { get; }

        [NotNull]
        public ICommand OpenSettingsCommand { get; }

        [NotNull]
        public ICommand SearchCommand { get; }

        protected override void Cleanup()
        {
            _translationList.CollectionChanged -= TranslationList_CollectionChanged;
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
            foreach (var token in _subscriptionTokens)
                _messenger.UnSubscribe(token);
        }

        private void Delete([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            // TODO: prompt
            Logger.Trace($"Deleting {translationEntryViewModel} from the list...");
            if (translationEntryViewModel == null)
                throw new ArgumentNullException(nameof(translationEntryViewModel));

            var deleted = _translationList.Remove(translationEntryViewModel);
            translationEntryViewModel.TextChanged -= TranslationEntryViewModel_TextChanged;
            if (!deleted)
                Logger.Warn($"{translationEntryViewModel} is not deleted from the list");
            else
                Logger.Trace($"{translationEntryViewModel} has been deleted from the list");
        }

        private async void LoadTranslationsAsync()
        {
            var pageNumber = 0;
            while (true)
            {
                var result = await LoadTranslationsPageAsync(pageNumber++).ConfigureAwait(false);
                if (!result)
                    break;
            }
        }

        private async Task<bool> LoadTranslationsPageAsync(int pageNumber)
        {
            return await Task.Run(
                    () =>
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                            return false;
                        Logger.Trace($"Receiving translations page {pageNumber}...");
                        var translationEntryViewModels = _viewModelAdapter.Adapt<TranslationEntryViewModel[]>(_translationEntryRepository.GetPage(pageNumber, PageSize, null, SortOrder.Descending));
                        if (!translationEntryViewModels.Any())
                            return false;

                        foreach (var translationEntryViewModel in translationEntryViewModels)
                            _translationList.Add(translationEntryViewModel);

                        Logger.Trace($"{translationEntryViewModels.Length} translations have been received");
                        return true;
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            Logger.Trace($"Changing priority for {priorityWordViewModel} in the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.TranslationEntryId;
            var translationEntryViewModel = _translationList.SingleOrDefault(x => Equals(x.Id, parentId));

            if (translationEntryViewModel == null)
            {
                Logger.Warn($"{priorityWordViewModel} is not found in the list");
                return;
            }

            if (priorityWordViewModel.IsPriority)
                ProcessPriority(priorityWordViewModel, translationEntryViewModel);
            else
                ProcessNonPriority(priorityWordViewModel, translationEntryViewModel);
        }

        private void OnUiLanguageChanged([NotNull] Language uiLanguage)
        {
            Logger.Trace($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage.Code);

            foreach (var translation in _translationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                translation.ReRender();
        }

        private void OnWordReceived([NotNull] TranslationInfo translationInfo)
        {
            if (translationInfo == null)
                throw new ArgumentNullException(nameof(translationInfo));

            Logger.Trace($"Received {translationInfo} from external source...");
            var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationInfo.TranslationEntry);

            var existing = _translationList.SingleOrDefault(x => Equals(x.Id, translationInfo.TranslationEntry.Id));
            if (existing != null)
            {
                Logger.Trace($"Updating {existing} in the list...");

                // Prevent text change to fire
                using (existing.SupressNotification())
                {
                    _viewModelAdapter.Adapt(translationInfo.TranslationEntry, existing);
                }

                _syncContext.Post(x => View.MoveCurrentTo(existing), null);
                Logger.Trace($"{existing} has been updated in the list");
            }
            else
            {
                Logger.Trace($"Adding {translationEntryViewModel} to the list...");
                _syncContext.Post(
                    x =>
                    {
                        _translationList.Insert(0, translationEntryViewModel);
                        View.MoveCurrentTo(translationEntryViewModel);
                    },
                    null);
                Logger.Trace($"{translationEntryViewModel} has been added to the list...");
            }
        }

        private void OnWordsBatchReceived([NotNull] TranslationInfo[] translationInfos)
        {
            Logger.Trace($"Received a batch of translations ({translationInfos.Length} items) from external source...");
            foreach (var translationInfo in translationInfos)
                OnWordReceived(translationInfo);
        }

        private void OpenDetails([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            Logger.Trace($"Opening details for {translationEntryViewModel}...");
            if (translationEntryViewModel == null)
                throw new ArgumentNullException(nameof(translationEntryViewModel));

            var translationEntry = _viewModelAdapter.Adapt<TranslationEntry>(translationEntryViewModel);
            var translationDetails = _wordsProcessor.ReloadTranslationDetailsIfNeeded(translationEntry.Id, translationEntry.Key.Text, translationEntry.Key.SourceLanguage, translationEntry.Key.TargetLanguage);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
            var translationResultCardViewModel = _lifetimeScope.Resolve<TranslationResultCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            var detailsWindow = _lifetimeScope.Resolve<ITranslationResultCardWindow>(
                new TypedParameter(typeof(Window), dictionaryWindow),
                new TypedParameter(typeof(TranslationResultCardViewModel), translationResultCardViewModel));
            detailsWindow.Show();
        }

        private void OpenSettings()
        {
            Logger.Trace("Opening settings...");
            var dictionaryWindow = _lifetimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            _lifetimeScope.Resolve<WindowFactory<ISettingsWindow>>().ShowWindow(dictionaryWindowParameter);
        }

        private void ProcessNonPriority([NotNull] PriorityWordViewModel priorityWordViewModel, [NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            var translations = translationEntryViewModel.Translations;
            Logger.Trace("Removing non-priority word from the list...");
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];

                if (_wordsEqualityComparer.Equals(translation, priorityWordViewModel))
                {
                    Logger.Trace($"Removing {translation} from the list...");
                    translations.RemoveAt(i--);
                }
            }

            if (!translations.Any())
            {
                Logger.Trace("No more translations left in the list. Restoring default...");
                translationEntryViewModel.ReloadNonPriority();
            }
        }

        private void ProcessPriority([NotNull] PriorityWordViewModel priorityWordViewModel, [NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            var translations = translationEntryViewModel.Translations;
            Logger.Trace($"Removing all non-priority translations for {translationEntryViewModel} except the current...");
            var found = false;
            for (var i = 0; i < translations.Count; i++)
            {
                var translation = translations[i];
                if (_wordsEqualityComparer.Equals(translation, priorityWordViewModel))
                {
                    if (!translation.IsPriority)
                    {
                        Logger.Debug($"Found {priorityWordViewModel} in the list. Marking as priority...");
                        translation.IsPriority = true;
                    }
                    else
                    {
                        Logger.Trace($"Found {priorityWordViewModel} in the list but it is already priority");
                    }
                    found = true;
                }

                if (!translation.IsPriority)
                    translations.RemoveAt(i--);
            }

            if (!found)
            {
                Logger.Trace($"Not found {priorityWordViewModel} in the list. Adding...");
                var copy = _viewModelAdapter.Adapt<PriorityWordViewModel>(priorityWordViewModel);
                translations.Add(copy);
            }
        }

        private void Search([CanBeNull] string text)
        {
            Logger.Trace($"Searching for {text}...");
            View.Filter = o => string.IsNullOrWhiteSpace(text) || ((TranslationEntryViewModel) o).Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0;
            Count = View.Cast<object>().Count();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var translation in _translationList)
            {
                var time = translation.NextCardShowTime;
                translation.NextCardShowTime = time.AddTicks(1); // To launch converter
            }
        }

        private void TranslationEntryViewModel_TextChanged([NotNull] object sender, [NotNull] TextChangedEventArgs e)
        {
            var translationEntryViewModel = (TranslationEntryViewModel) sender;
            Logger.Info($"Changing translation's text for {translationEntryViewModel} to {e.NewValue}...");

            var sourceLanguage = translationEntryViewModel.Language;
            var targetLanguage = translationEntryViewModel.TargetLanguage;
            if (e.NewValue != null)
                WordsProcessor.AddOrChangeWord(e.NewValue, sourceLanguage, targetLanguage, Window, id: translationEntryViewModel.Id);
        }

        private void TranslationList_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            //TODO: just increment/decrement, but take view into an account
            Count = View.Cast<object>().Count();
            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (TranslationEntryViewModel translationEntryViewModel in e.OldItems)
                {
                    _translationDetailsRepository.DeleteByTranslationEntryId(translationEntryViewModel.Id);
                    _translationEntryRepository.Delete(translationEntryViewModel.Id);
                }
            else if (e.Action == NotifyCollectionChangedAction.Add)
                foreach (TranslationEntryViewModel translationEntryViewModel in e.NewItems)
                    translationEntryViewModel.TextChanged += TranslationEntryViewModel_TextChanged;
        }
    }
}