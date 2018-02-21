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
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
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
        private const int PageSize = 15;

        [NotNull]
        private readonly IDialogService _dialogService;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly DispatcherTimer _timer;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly ObservableCollection<TranslationEntryViewModel> _translationList;

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
            [NotNull] IMessageHub messageHub,
            [NotNull] EditManualTranslationsViewModel editManualTranslationsViewModel,
            [NotNull] IDialogService dialogService,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(localSettingsRepository, languageDetector, translationEntryProcessor, logger)
        {
            EditManualTranslationsViewModel = editManualTranslationsViewModel ?? throw new ArgumentNullException(nameof(editManualTranslationsViewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));

            FavoriteCommand = new CorrelationCommand<TranslationEntryViewModel>(Favorite);
            DeleteCommand = new AsyncCorrelationCommand<TranslationEntryViewModel>(DeleteAsync);
            OpenDetailsCommand = new AsyncCorrelationCommand<TranslationEntryViewModel>(OpenDetailsAsync);
            OpenSettingsCommand = new AsyncCorrelationCommand(OpenSettingsAsync);
            SearchCommand = new CorrelationCommand<string>(Search);
            WindowContentRenderedCommand = new AsyncCorrelationCommand(WindowContentRenderedAsync);

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

            _subscriptionTokens.Add(messageHub.Subscribe<TranslationEntry>(OnTranslationInfoReceivedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<ICollection<TranslationEntry>>(OnTranslationInfosBatchReceivedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<PriorityWordKey>(OnPriorityChangedAsync));

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

        [NotNull]
        public ICommand WindowContentRenderedCommand { get; }

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

                        translationEntryViewModel.ProcessPriorityChange(priorityWordKey);
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
                _messageHub.UnSubscribe(subscriptionToken);
            }
        }

        private async Task DeleteAsync([NotNull] TranslationEntryViewModel translationEntryViewModel)
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
            var learningInfo = _learningInfoRepository.GetOrInsert(translationEntryViewModel.Id);
            learningInfo.IsFavorited = translationEntryViewModel.IsFavorited;
            _learningInfoRepository.Update(learningInfo);
        }

        private async Task LoadTranslationsAsync()
        {
            await Task.Run(
                    async () =>
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
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private async Task<bool> LoadTranslationsPageAsync(int pageNumber)
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            Logger.TraceFormat("Receiving translations page {0}...", pageNumber);
            var translationEntries = _translationEntryRepository.GetPage(pageNumber, PageSize, nameof(TranslationEntry.ModifiedDate), SortOrder.Descending);
            if (!translationEntries.Any())
            {
                return false;
            }

            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var viewModels = translationEntries.Select(translationEntry => _lifetimeScope.Resolve<TranslationEntryViewModel>(new TypedParameter(typeof(TranslationEntry), translationEntry)));
            _synchronizationContext.Send(
                x =>
                {
                    foreach (var translationEntryViewModel in viewModels)
                    {
                        _translationList.Add(translationEntryViewModel);
                    }

                    UpdateCount();
                },
                null);

            Logger.DebugFormat("{0} translations have been received", translationEntries.Length);
            _semaphore.Release();
            return true;
        }

        private async void OnTranslationInfoReceivedAsync([NotNull] TranslationEntry translationEntry)
        {
            if (translationEntry == null)
            {
                throw new ArgumentNullException(nameof(translationEntry));
            }

            Logger.DebugFormat("Received {0} from external source", translationEntry);

            await Task.Run(async () => await ProcessNewTranslationAsync(translationEntry).ConfigureAwait(false), CancellationTokenSource.Token).ConfigureAwait(false);
        }

        private async Task ProcessNewTranslationAsync([NotNull] TranslationEntry translationEntry)
        {
            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var existingTranslationEntryViewModel = _translationList.SingleOrDefault(x => x.Id.Equals(translationEntry.Id));
            _semaphore.Release();

            if (existingTranslationEntryViewModel != null)
            {
                Logger.TraceFormat("Updating {0} in the list...", existingTranslationEntryViewModel);
                var learningInfo = _learningInfoRepository.GetOrInsert(translationEntry.Id);
                //no await here
                existingTranslationEntryViewModel.UpdateAsync(translationEntry, learningInfo);

                _synchronizationContext.Post(x => View.MoveCurrentTo(existingTranslationEntryViewModel), null);
                Logger.DebugFormat("{0} has been updated in the list", existingTranslationEntryViewModel);
            }
            else
            {
                Logger.TraceFormat("Adding {0} to the list...", translationEntry);
                var translationEntryViewModel = _lifetimeScope.Resolve<TranslationEntryViewModel>(new TypedParameter(typeof(TranslationEntry), translationEntry));
                await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                _synchronizationContext.Send(
                    x =>
                    {
                        _translationList.Insert(0, translationEntryViewModel);
                        _semaphore.Release();

                        Logger.DebugFormat("{0} has been added to the list...", translationEntryViewModel);
                        View.MoveCurrentTo(translationEntryViewModel);
                    },
                    null);
            }
        }

        private async void OnTranslationInfosBatchReceivedAsync([NotNull] ICollection<TranslationEntry> translationEntries)
        {
            if (translationEntries == null)
            {
                throw new ArgumentNullException(nameof(translationEntries));
            }

            Logger.DebugFormat("Received a batch of translations ({0} items) from the external source...", translationEntries.Count);

            await Task.Run(
                    async () =>
                    {
                        foreach (var translationEntry in translationEntries)
                        {
                            await ProcessNewTranslationAsync(translationEntry).ConfigureAwait(false);
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

        private async Task WindowContentRenderedAsync()
        {
            await LoadTranslationsAsync().ConfigureAwait(false);
        }

        private async Task OpenDetailsAsync([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            if (translationEntryViewModel == null)
            {
                throw new ArgumentNullException(nameof(translationEntryViewModel));
            }

            Logger.TraceFormat("Opening details for {0}...", translationEntryViewModel);

            var translationEntry = _translationEntryRepository.GetById(translationEntryViewModel.Id);
            var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationTokenSource.Token).ConfigureAwait(false);
            var learningInfo = _learningInfoRepository.GetOrInsert(translationEntry.Id);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails, learningInfo);
            var nestedLifeTimeScope = _lifetimeScope.BeginLifetimeScope();
            var translationDetailsCardViewModel = nestedLifeTimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var dictionaryWindow = await nestedLifeTimeScope.Resolve<WindowFactory<IDictionaryWindow>>().GetWindowAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var detailsWindow = nestedLifeTimeScope.Resolve<ITranslationDetailsCardWindow>(
                new TypedParameter(typeof(Window), dictionaryWindow),
                new TypedParameter(typeof(TranslationDetailsCardViewModel), translationDetailsCardViewModel));
            detailsWindow.AssociateDisposable(nestedLifeTimeScope);
            detailsWindow.Show();
        }

        private async Task OpenSettingsAsync()
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
                        TranslationEntryProcessor.DeleteTranslationEntry(translationEntryViewModel.Id);
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