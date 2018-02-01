using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.View.Card;
using Remembrance.Contracts.View.Settings;
using Remembrance.Resources;
using Remembrance.ViewModel.Card;
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
        private readonly IDialogService _dialogService;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly object _lockObject = new object();

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        [NotNull]
        private readonly DispatcherTimer _timer;

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly ObservableCollection<TranslationEntryViewModel> _translationList;

        [NotNull]
        private readonly IViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        private int _count;
        private bool _filterChanged;
        private int _lastRecordedCount;

        public DictionaryViewModel(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] EditManualTranslationsViewModel editManualTranslationsViewModel,
            [NotNull] IDialogService dialogService)
            : base(localSettingsRepository, languageDetector, translationEntryProcessor, logger)
        {
            EditManualTranslationsViewModel = editManualTranslationsViewModel ?? throw new ArgumentNullException(nameof(editManualTranslationsViewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));

            FavoriteCommand = new CorrelationCommand<TranslationEntryViewModel>(Favorite);
            DeleteCommand = new CorrelationCommand<TranslationEntryViewModel>(Delete);
            OpenDetailsCommand = new CorrelationCommand<TranslationEntryViewModel>(OpenDetailsAsync);
            OpenSettingsCommand = new CorrelationCommand(OpenSettings);
            SearchCommand = new CorrelationCommand<string>(Search);

            Logger.Info("Starting...");

            _translationList = new ObservableCollection<TranslationEntryViewModel>();
            _translationList.CollectionChanged += TranslationList_CollectionChanged;
            View = CollectionViewSource.GetDefaultView(_translationList);

            BindingOperations.EnableCollectionSynchronization(_translationList, _lockObject);

            Logger.Trace("Creating NextCardShowTime update timer...");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;

            Logger.Trace("Subscribing to the events...");

            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo>(OnTranslationInfoReceived));
            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo[]>(OnTranslationInfosBatchReceived));
            _subscriptionTokens.Add(messenger.Subscribe<CultureInfo>(OnUiLanguageChanged));

            Logger.Trace("Receiving translations...");

            LoadTranslationsAsync();

            Logger.Info("Started");
        }

        [NotNull]
        public EditManualTranslationsViewModel EditManualTranslationsViewModel { get; }

        protected override IWindow Window => _dictionaryWindowFactory.GetWindowIfExists();

        [NotNull]
        public ICollectionView View { get; }

        public int Count { get; private set; }

        [CanBeNull]
        public string SearchText { get; set; }

        [NotNull]
        public ICommand FavoriteCommand { get; }

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
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messenger.UnSubscribe(subscriptionToken);
            }
        }

        private void Delete([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            if (!_dialogService.ConfirmDialog(string.Format(Texts.AreYouSureDelete, translationEntryViewModel)))
            {
                return;
            }

            Logger.Trace($"Deleting {translationEntryViewModel} from the list...");
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            bool deleted;
            lock (_lockObject)
            {
                deleted = _translationList.Remove(translationEntryViewModel);
            }

            if (!deleted)
            {
                Logger.Warn($"{translationEntryViewModel} is not deleted from the list");
            }
            else
            {
                Logger.Trace($"{translationEntryViewModel} has been deleted from the list");
            }
        }

        private void Favorite([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            var text = translationEntryViewModel.IsFavorited
                ? "Unfavoriting"
                : "Favoriting";
            Logger.Trace($"{text} {translationEntryViewModel}...");
            translationEntryViewModel.IsFavorited = !translationEntryViewModel.IsFavorited;
            var translationEntry = _viewModelAdapter.Adapt<TranslationEntry>(translationEntryViewModel);
            _translationEntryRepository.Update(translationEntry);
        }

        private async void LoadTranslationsAsync()
        {
            var pageNumber = 0;
            while (true)
            {
                var result = await LoadTranslationsPageAsync(pageNumber++).ConfigureAwait(false);
                if (!result)
                {
                    break;
                }
            }
        }

        private async Task<bool> LoadTranslationsPageAsync(int pageNumber)
        {
            return await Task.Run(
                    () =>
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            return false;
                        }

                        Logger.Trace($"Receiving translations page {pageNumber}...");
                        var translationEntryViewModels = _viewModelAdapter.Adapt<TranslationEntryViewModel[]>(_translationEntryRepository.GetPage(pageNumber, PageSize, null, SortOrder.Descending));
                        if (!translationEntryViewModels.Any())
                        {
                            return false;
                        }

                        lock (_lockObject)
                        {
                            foreach (var translationEntryViewModel in translationEntryViewModels)
                            {
                                _translationList.Add(translationEntryViewModel);
                            }
                        }

                        Logger.Trace($"{translationEntryViewModels.Length} translations have been received");
                        return true;
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private void OnTranslationInfoReceived([NotNull] TranslationInfo translationInfo)
        {
            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            Logger.Trace($"Received {translationInfo} from external source...");
            var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationInfo.TranslationEntry);

            TranslationEntryViewModel existing;
            lock (_lockObject)
            {
                existing = _translationList.SingleOrDefault(x => x.Id.Equals(translationInfo.TranslationEntry.Id));
            }

            if (existing != null)
            {
                Logger.Trace($"Updating {existing} in the list...");

                // Prevent text change to fire
                using (existing.SupressNotification())
                {
                    _viewModelAdapter.Adapt(translationInfo.TranslationEntry, existing);
                    existing.ReloadTranslationsAsync().ConfigureAwait(false);
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
                        lock (_lockObject)
                        {
                            _translationList.Insert(0, translationEntryViewModel);
                        }

                        View.MoveCurrentTo(translationEntryViewModel);
                    },
                    null);
                Logger.Trace($"{translationEntryViewModel} has been added to the list...");
            }
        }

        private void OnTranslationInfosBatchReceived([NotNull] TranslationInfo[] translationInfos)
        {
            Logger.Trace($"Received a batch of translations ({translationInfos.Length} items) from external source...");
            foreach (var translationInfo in translationInfos)
            {
                OnTranslationInfoReceived(translationInfo);
            }
        }

        private void OnUiLanguageChanged([NotNull] CultureInfo cultureInfo)
        {
            Logger.Trace($"Changing UI language to {cultureInfo}...");
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            CultureUtilities.ChangeCulture(cultureInfo);

            lock (_lockObject)
            {
                foreach (var translation in _translationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                {
                    translation.ReRender();
                }
            }
        }

        private async void OpenDetailsAsync([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            Logger.Trace($"Opening details for {translationEntryViewModel}...");
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            var translationEntry = _viewModelAdapter.Adapt<TranslationEntry>(translationEntryViewModel);
            var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationTokenSource.Token).ConfigureAwait(false);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
            var nestedLifeTimeScope = _lifetimeScope.BeginLifetimeScope();
            var translationDetailsCardViewModel = nestedLifeTimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var dictionaryWindow = nestedLifeTimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            var detailsWindow = nestedLifeTimeScope.Resolve<ITranslationDetailsCardWindow>(
                new TypedParameter(typeof(Window), dictionaryWindow),
                new TypedParameter(typeof(TranslationDetailsCardViewModel), translationDetailsCardViewModel));
            detailsWindow.AssociateDisposable(nestedLifeTimeScope);
            detailsWindow.Show();
        }

        private void OpenSettings()
        {
            Logger.Trace("Opening settings...");
            var nestedLifeTimeScope = _lifetimeScope.BeginLifetimeScope();
            var dictionaryWindow = nestedLifeTimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindow();
            dictionaryWindow.AssociateDisposable(nestedLifeTimeScope);
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            var windowFactory = nestedLifeTimeScope.Resolve<WindowFactory<ISettingsWindow>>();
            windowFactory.ShowWindow(dictionaryWindowParameter);
        }

        private void Search([CanBeNull] string text)
        {
            Logger.Trace($"Searching for {text}...");
            if (string.IsNullOrWhiteSpace(text))
            {
                View.Filter = null;
            }
            else
            {
                View.Filter = o => string.IsNullOrWhiteSpace(text) || ((TranslationEntryViewModel)o).WordText.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0;
            }

            _filterChanged = true;
            //Count = View.Cast<object>().Count();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var translation in _translationList)
            {
                var time = translation.NextCardShowTime;
                translation.NextCardShowTime = time.AddTicks(1); // To launch converter
            }

            UpdateCount();
        }

        private void UpdateCount()
        {
            if (!_filterChanged && _count == _lastRecordedCount)
            {
                return;
            }

            _filterChanged = false;
            _lastRecordedCount = _count;

            Count = View.Filter == null
                ? _count
                : View.Cast<object>().Count();
        }

        private void TranslationList_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (TranslationEntryViewModel translationEntryViewModel in e.OldItems)
                    {
                        Interlocked.Decrement(ref _count);
                        _translationEntryProcessor.DeleteTranslationEntry(translationEntryViewModel.Id);
                    }

                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (TranslationEntryViewModel translationEntryViewModel in e.NewItems)
                    {
                        Interlocked.Increment(ref _count);
                    }

                    break;
            }
        }
    }
}