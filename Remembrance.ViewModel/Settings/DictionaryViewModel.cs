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
using Remembrance.Contracts.CardManagement.Data;
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
        private const int PageSize = 50;

        [NotNull]
        private readonly IDialogService _dialogService;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

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
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] EditManualTranslationsViewModel editManualTranslationsViewModel,
            [NotNull] IDialogService dialogService,
            [NotNull] SynchronizationContext synchronizationContext)
            : base(localSettingsRepository, languageDetector, translationEntryProcessor, logger)
        {
            EditManualTranslationsViewModel = editManualTranslationsViewModel ?? throw new ArgumentNullException(nameof(editManualTranslationsViewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));

            FavoriteCommand = new CorrelationCommand<TranslationEntryViewModel>(Favorite);
            DeleteCommand = new CorrelationCommand<TranslationEntryViewModel>(DeleteAsync);
            OpenDetailsCommand = new CorrelationCommand<TranslationEntryViewModel>(OpenDetailsAsync);
            OpenSettingsCommand = new CorrelationCommand(OpenSettingsAsync);
            SearchCommand = new CorrelationCommand<string>(Search);

            Logger.Trace("Starting...");

            _translationList = new ObservableCollection<TranslationEntryViewModel>();
            _translationList.CollectionChanged += TranslationList_CollectionChanged;
            View = CollectionViewSource.GetDefaultView(_translationList);

            Logger.Trace("Creating NextCardShowTime update timer...");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Start();
            _timer.Tick += Timer_Tick;

            Logger.Trace("Subscribing to the events...");

            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo>(OnTranslationInfoReceivedAsync));
            _subscriptionTokens.Add(messenger.Subscribe<TranslationInfo[]>(OnTranslationInfosBatchReceivedAsync));
            _subscriptionTokens.Add(messenger.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messenger.Subscribe<PriorityWordKey>(OnPriorityChangedAsync));

            LoadTranslationsAsync();

            Logger.Debug("Started");
        }

        [NotNull]
        public EditManualTranslationsViewModel EditManualTranslationsViewModel { get; }

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

        private async void OnPriorityChangedAsync([NotNull] PriorityWordKey priorityWordKey)
        {
            if (priorityWordKey == null)
            {
                throw new ArgumentNullException(nameof(priorityWordKey));
            }

            Logger.TraceFormat("Changing priority: {0}...", priorityWordKey);

            await Task.Run(
                    async () =>
                    {
                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        var translationEntryViewModel = _translationList.SingleOrDefault(x => x.Id.Equals(priorityWordKey.WordKey.TranslationEntryKey));
                        _semaphore.Release();

                        if (translationEntryViewModel == null)
                        {
                            Logger.WarnFormat("Cannot find {0} in translations list", priorityWordKey.WordKey);
                            return;
                        }

                        translationEntryViewModel.ProcessPriority(priorityWordKey);
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        protected override async Task<IWindow> GetWindowAsync()
        {
            return await _dictionaryWindowFactory.GetWindowIfExistsAsync(CancellationTokenSource.Token).ConfigureAwait(false);
        }

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

        private async void DeleteAsync([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            if (!_dialogService.ConfirmDialog(string.Format(Texts.AreYouSureDelete, translationEntryViewModel)))
            {
                return;
            }

            Logger.TraceFormat("Deleting {0} from the list...", translationEntryViewModel);

            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var deleted = _translationList.Remove(translationEntryViewModel);
            _semaphore.Release();

            if (!deleted)
            {
                Logger.WarnFormat("{0} is not deleted from the list", translationEntryViewModel);
            }
            else
            {
                Logger.DebugFormat("{0} has been deleted from the list", translationEntryViewModel);
            }
        }

        private void Favorite([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            Logger.TraceFormat(
                "{0} {1}...",
                translationEntryViewModel.IsFavorited
                    ? "Unfavoriting"
                    : "Favoriting",
                translationEntryViewModel);
            translationEntryViewModel.IsFavorited = !translationEntryViewModel.IsFavorited;
            var translationEntry = _translationEntryRepository.GetById(translationEntryViewModel.Id);
            translationEntry.IsFavorited = translationEntryViewModel.IsFavorited;
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
                    async () =>
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            return false;
                        }

                        Logger.TraceFormat("Receiving translations page {0}...", pageNumber);
                        var translationEntryViewModels =
                            _viewModelAdapter.Adapt<TranslationEntryViewModel[]>(_translationEntryRepository.GetPage(pageNumber, PageSize, nameof(TranslationEntry.ModifiedDate), SortOrder.Descending));
                        if (!translationEntryViewModels.Any())
                        {
                            return false;
                        }

                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        _synchronizationContext.Send(
                            x =>
                            {
                                foreach (var translationEntryViewModel in translationEntryViewModels)
                                {
                                    _translationList.Add(translationEntryViewModel);
                                }

                                Logger.DebugFormat("{0} translations have been received", translationEntryViewModels.Length);
                            },
                            null);
                        _semaphore.Release();

                        return true;
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private async void OnTranslationInfoReceivedAsync([NotNull] TranslationInfo translationInfo)
        {
            if (translationInfo == null)
            {
                throw new ArgumentNullException(nameof(translationInfo));
            }

            Logger.DebugFormat("Received {0} from external source", translationInfo);

            await Task.Run(async () => await ProcessNewTranslationAsync(translationInfo).ConfigureAwait(false), CancellationTokenSource.Token).ConfigureAwait(false);
        }

        private async Task ProcessNewTranslationAsync([NotNull] TranslationInfo translationInfo)
        {
            var translationEntryViewModel = _viewModelAdapter.Adapt<TranslationEntryViewModel>(translationInfo.TranslationEntry);

            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var existing = _translationList.SingleOrDefault(x => x.Id.Equals(translationInfo.TranslationEntry.Id));
            _semaphore.Release();

            if (existing != null)
            {
                Logger.TraceFormat("Updating {0} in the list...", existing);

                // Prevent text change to fire
                using (existing.SupressNotification())
                {
                    _viewModelAdapter.Adapt(translationInfo.TranslationEntry, existing);
                }

                _synchronizationContext.Post(x => View.MoveCurrentTo(existing), null);
                Logger.DebugFormat("{0} has been updated in the list", existing);
            }
            else
            {
                Logger.TraceFormat("Adding {0} to the list...", translationEntryViewModel);
                await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                _synchronizationContext.Send(
                    x =>
                    {
                        _translationList.Insert(0, translationEntryViewModel);
                        _semaphore.Release();

                        Logger.DebugFormat("{0} has been added to the list...", translationEntryViewModel);
                    },
                    null);
                View.MoveCurrentTo(translationEntryViewModel);
            }
        }

        private async void OnTranslationInfosBatchReceivedAsync([NotNull] TranslationInfo[] translationInfos)
        {
            if (translationInfos == null)
            {
                throw new ArgumentNullException(nameof(translationInfos));
            }

            Logger.DebugFormat("Received a batch of translations ({0} items) from the external source...", translationInfos.Length);

            await Task.Run(
                    async () =>
                    {
                        foreach (var translationInfo in translationInfos)
                        {
                            await ProcessNewTranslationAsync(translationInfo).ConfigureAwait(false);
                        }
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private async void OnUiLanguageChangedAsync([NotNull] CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            Logger.TraceFormat("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    async () =>
                    {
                        CultureUtilities.ChangeCulture(cultureInfo);

                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        foreach (var translation in _translationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                        {
                            translation.ReRender();
                        }

                        _semaphore.Release();
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private async void OpenDetailsAsync([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            Logger.TraceFormat("Opening details for {0}...", translationEntryViewModel);

            var translationEntry = _translationEntryRepository.GetById(translationEntryViewModel.Id);
            var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationTokenSource.Token).ConfigureAwait(false);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
            var nestedLifeTimeScope = _lifetimeScope.BeginLifetimeScope();
            var translationDetailsCardViewModel = nestedLifeTimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var dictionaryWindow = await nestedLifeTimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var detailsWindow = nestedLifeTimeScope.Resolve<ITranslationDetailsCardWindow>(
                new TypedParameter(typeof(Window), dictionaryWindow),
                new TypedParameter(typeof(TranslationDetailsCardViewModel), translationDetailsCardViewModel));
            detailsWindow.AssociateDisposable(nestedLifeTimeScope);
            detailsWindow.Show();
        }

        private async void OpenSettingsAsync()
        {
            Logger.Trace("Opening settings...");

            var nestedLifeTimeScope = _lifetimeScope.BeginLifetimeScope();
            var dictionaryWindow = await nestedLifeTimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            dictionaryWindow.AssociateDisposable(nestedLifeTimeScope);
            var dictionaryWindowParameter = new TypedParameter(typeof(Window), dictionaryWindow);
            var windowFactory = nestedLifeTimeScope.Resolve<WindowFactory<ISettingsWindow>>();
            await windowFactory.ShowWindowAsync(CancellationTokenSource.Token, dictionaryWindowParameter).ConfigureAwait(false);
        }

        private void Search([CanBeNull] string text)
        {
            Logger.TraceFormat("Searching for {0}...", text);

            if (string.IsNullOrWhiteSpace(text))
            {
                View.Filter = null;
            }
            else
            {
                View.Filter = o => string.IsNullOrWhiteSpace(text) || ((TranslationEntryViewModel)o).Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0;
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