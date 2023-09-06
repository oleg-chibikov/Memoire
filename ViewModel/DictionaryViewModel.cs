using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Languages;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View;
using Mémoire.Contracts.View.Card;
using Mémoire.Contracts.View.Settings;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.DAL.Contracts;
using Scar.Common.Localization;
using Scar.Common.MVVM.CollectionView;
using Scar.Common.MVVM.Commands;
using Scar.Common.RateLimiting;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class DictionaryViewModel : BaseViewModelWithAddTranslationControl
    {
        readonly ILogger _logger;
        readonly Func<string, bool, ConfirmationViewModel> _confirmationViewModelFactory;
        readonly Func<ConfirmationViewModel, IConfirmationWindow> _confirmationWindowFactory;
        readonly ICultureManager _cultureManager;
        readonly IWindowFactory<IDictionaryWindow> _dictionaryWindowFactory;
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly IMessageHub _messageHub;
        readonly IRateLimiter _rateLimiter;
        readonly IScopedWindowProvider _scopedWindowProvider;
        readonly IWindowFactory<ISettingsWindow> _settingsWindowFactory;
        readonly IList<Guid> _subscriptionTokens = new List<Guid>();
        readonly SynchronizationContext _synchronizationContext;
#pragma warning disable CA2213 // They are disposed in the Cleanup method
        readonly SemaphoreSlim _semaphore = new (1, 1);
        readonly Timer _timer;
#pragma warning restore CA2213
        readonly ITranslationEntryRepository _translationEntryRepository;
        readonly Func<TranslationEntry, TranslationEntryViewModel> _translationEntryViewModelFactory;
        readonly ObservableCollection<TranslationEntryViewModel> _translationList;
        readonly IWindowPositionAdjustmentManager _windowPositionAdjustmentManager;
        int _count;
        bool _filterChanged;
        int _lastRecordedCount;
        bool _loaded;

        public DictionaryViewModel(
            ITranslationEntryRepository translationEntryRepository,
            ILocalSettingsRepository localSettingsRepository,
            ILanguageManager languageManager,
            ITranslationEntryProcessor translationEntryProcessor,
            ILogger<BaseViewModelWithAddTranslationControl> baseLogger,
            ILogger<DictionaryViewModel> logger,
            IWindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            IMessageHub messageHub,
            EditManualTranslationsViewModel editManualTranslationsViewModel,
            SynchronizationContext synchronizationContext,
            ILearningInfoRepository learningInfoRepository,
            Func<TranslationEntry, TranslationEntryViewModel> translationEntryViewModelFactory,
            IScopedWindowProvider scopedWindowProvider,
            IWindowFactory<ISettingsWindow> settingsWindowFactory,
            IRateLimiter rateLimiter,
            Func<string, bool, ConfirmationViewModel> confirmationViewModelFactory,
            Func<ConfirmationViewModel, IConfirmationWindow> confirmationWindowFactory,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager,
            ICultureManager cultureManager,
            ICommandManager commandManager,
            ICollectionViewSource collectionViewSource) : base(localSettingsRepository, languageManager, translationEntryProcessor, baseLogger, commandManager)
        {
            EditManualTranslationsViewModel = editManualTranslationsViewModel ?? throw new ArgumentNullException(nameof(editManualTranslationsViewModel));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryViewModelFactory = translationEntryViewModelFactory ?? throw new ArgumentNullException(nameof(translationEntryViewModelFactory));
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _settingsWindowFactory = settingsWindowFactory ?? throw new ArgumentNullException(nameof(settingsWindowFactory));
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _confirmationViewModelFactory = confirmationViewModelFactory ?? throw new ArgumentNullException(nameof(confirmationViewModelFactory));
            _confirmationWindowFactory = confirmationWindowFactory ?? throw new ArgumentNullException(nameof(confirmationWindowFactory));
            _windowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = collectionViewSource ?? throw new ArgumentNullException(nameof(collectionViewSource));

            DeleteCommand = AddCommand<TranslationEntryViewModel>(DeleteAsync);
            OpenDetailsCommand = AddCommand<TranslationEntryViewModel>(OpenDetailsAsync);
            OpenSettingsCommand = AddCommand(OpenSettingsAsync);
            SearchCommand = AddCommand<string>(Search);
            WindowContentRenderedCommand = AddCommand(WindowContentRenderedAsync);

            _translationList = new ObservableCollection<TranslationEntryViewModel>();
            _translationList.CollectionChanged += TranslationList_CollectionChanged;
            View = collectionViewSource.GetDefaultView(_translationList);

            _logger.LogTrace("Creating NextCardShowTime update timer...");

            _timer = new Timer(Timer_TickAsync, null, 0, 10000);

            _logger.LogTrace("Subscribing to the events...");

            _subscriptionTokens.Add(messageHub.Subscribe<TranslationEntry>(HandleTranslationEntryReceivedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<LearningInfo>(HandleLearningInfoReceivedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<IReadOnlyCollection<TranslationEntry>>(HandleTranslationEntriesBatchReceivedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(HandleUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<PriorityWordKey>(HandlePriorityChangedAsync));

            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public int Count { get; private set; }

        public ICommand DeleteCommand { get; }

        public EditManualTranslationsViewModel EditManualTranslationsViewModel { get; }

        public ICommand OpenDetailsCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand SearchCommand { get; }

        public string? SearchText { get; set; }

        public ICollectionView View { get; }

        public ICommand WindowContentRenderedCommand { get; }

        protected override void Cleanup()
        {
            try
            {
                _semaphore.Wait(CancellationTokenSource.Token);
                _semaphore.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Do nothing, it's a normal behavior
            }

            _translationList.CollectionChanged -= TranslationList_CollectionChanged;
            _timer.Dispose();
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messageHub.Unsubscribe(subscriptionToken);
            }

            _subscriptionTokens.Clear();
        }

        protected override async Task<IDisplayable?> GetWindowAsync()
        {
            return await _dictionaryWindowFactory.GetWindowIfExistsAsync(CancellationTokenSource.Token).ConfigureAwait(false);
        }

        async Task DeleteAsync(TranslationEntryViewModel translationEntryViewModel)
        {
            _ = translationEntryViewModel ?? throw new ArgumentNullException(nameof(translationEntryViewModel));
            if (!await ConfirmDeletionAsync(translationEntryViewModel.ToString()).ConfigureAwait(false))
            {
                return;
            }

            _logger.LogTrace("Deleting {Translation} from the list...", translationEntryViewModel);

            var deleted = false;
            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            _synchronizationContext.Send(
                _ =>
                {
                    deleted = _translationList.Remove(translationEntryViewModel);
                },
                null);
            _semaphore.Release();

            if (!deleted)
            {
                _logger.LogWarning("{Translation} is not deleted from the list", translationEntryViewModel);
            }
            else
            {
                _logger.LogDebug("{Translation} has been deleted from the list", translationEntryViewModel);
            }
        }

        async Task LoadTranslationsAsync()
        {
            await Task.Run(
                    async () =>
                    {
                        var pageNumber = 0;
                        bool result;
                        do
                        {
                            result = await LoadTranslationsPageAsync(pageNumber++).ConfigureAwait(false);
                        }
                        while (result);

                        _loaded = true;
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        async Task<bool> LoadTranslationsPageAsync(int pageNumber)
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return false;
            }

            _logger.LogTrace("Receiving translations page {PageNumber}...", pageNumber);
            var translationEntries = _translationEntryRepository.GetPage(pageNumber, AppSettings.DictionaryPageSize, nameof(TranslationEntry.ModifiedDate), SortOrder.Descending);
            if (!(translationEntries.Count > 0))
            {
                return false;
            }

            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var viewModels = translationEntries.Select(_translationEntryViewModelFactory);
            _synchronizationContext.Send(
                _ =>
                {
                    foreach (var translationEntryViewModel in viewModels)
                    {
                        _translationList.Add(translationEntryViewModel);
                    }

                    UpdateCount();
                },
                null);

            _semaphore.Release();
            _logger.LogDebug("{TranslationCount} translations have been received", translationEntries.Count);
            return true;
        }

        async void HandleLearningInfoReceivedAsync(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            _logger.LogDebug("Received {LearningInfo} from external source", learningInfo);

            await Task.Run(
                    async () =>
                    {
                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        var translationEntryViewModel = _translationList.SingleOrDefault(x => x.Id.Equals(learningInfo.Id));
                        _semaphore.Release();
                        if (translationEntryViewModel == null)
                        {
                            _logger.LogDebug("Translation entry is still not loaded for {LearningInfo}", learningInfo);
                            return;
                        }

                        _logger.LogTrace("Updating LearningInfo for {Translation} in the list...", translationEntryViewModel);
                        translationEntryViewModel.Update(learningInfo, translationEntryViewModel.ModifiedDate);
                        _logger.LogDebug("LearningInfo for {Translation} has been updated in the list", translationEntryViewModel);
                        ScrollTo(translationEntryViewModel);
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(true);
        }

        async void HandlePriorityChangedAsync(PriorityWordKey priorityWordKey)
        {
            _ = priorityWordKey ?? throw new ArgumentNullException(nameof(priorityWordKey));
            _logger.LogTrace("Changing priority: {PriorityWordKey}...", priorityWordKey);

            await Task.Run(
                    async () =>
                    {
                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        var translationEntryViewModel = _translationList.SingleOrDefault(x => x.Id.Equals(priorityWordKey.WordKey.Key));
                        _semaphore.Release();

                        if (translationEntryViewModel == null)
                        {
                            _logger.LogWarning("Cannot find {PriorityWordKey} in translations list", priorityWordKey.WordKey);
                            return;
                        }

                        translationEntryViewModel.ProcessPriorityChange(priorityWordKey);
                        ScrollTo(translationEntryViewModel);
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(true);
        }

        async void HandleTranslationEntriesBatchReceivedAsync(IReadOnlyCollection<TranslationEntry> translationEntries)
        {
            _ = translationEntries ?? throw new ArgumentNullException(nameof(translationEntries));
            _logger.LogDebug("Received a batch of translations ({TranslationCount} items) from the external source...", translationEntries.Count);

            await Task.Run(
                    async () =>
                    {
                        foreach (var translationEntry in translationEntries)
                        {
                            await OnTranslationEntryReceivedInternalAsync(translationEntry).ConfigureAwait(false);
                        }
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(true);
        }

        async void HandleTranslationEntryReceivedAsync(TranslationEntry translationEntry)
        {
            _ = translationEntry ?? throw new ArgumentNullException(nameof(translationEntry));
            _logger.LogDebug("Received {Translation} from external source", translationEntry);

            await Task.Run(async () => await OnTranslationEntryReceivedInternalAsync(translationEntry).ConfigureAwait(false), CancellationTokenSource.Token).ConfigureAwait(true);
        }

        async Task OnTranslationEntryReceivedInternalAsync(TranslationEntry translationEntry)
        {
            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var existingTranslationEntryViewModel = _translationList.SingleOrDefault(x => x.Id.Equals(translationEntry.Id));
            _semaphore.Release();

            if (existingTranslationEntryViewModel != null)
            {
                _logger.LogTrace("Updating {Translation} in the list...", existingTranslationEntryViewModel);
                existingTranslationEntryViewModel.HasManualTranslations = translationEntry.ManualTranslations?.Count > 0;
                var learningInfo = _learningInfoRepository.GetOrInsert(translationEntry.Id);
                existingTranslationEntryViewModel.Update(learningInfo, translationEntry.ModifiedDate);

                // no await here
                // ReSharper disable once AssignmentIsFullyDiscarded
                _ = existingTranslationEntryViewModel.ReloadTranslationsAsync(translationEntry);

                _logger.LogDebug("{Translation} has been updated in the list", existingTranslationEntryViewModel);
                ScrollTo(existingTranslationEntryViewModel);
            }
            else
            {
                _logger.LogTrace("Adding {Translation} to the list...", translationEntry);
                var translationEntryViewModel = _translationEntryViewModelFactory(translationEntry);
                await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                _synchronizationContext.Send(_ => _translationList.Insert(0, translationEntryViewModel), null);
                _semaphore.Release();

                _logger.LogDebug("{Translation} has been added to the list...", translationEntryViewModel);
                ScrollTo(translationEntryViewModel);
            }
        }

        async void HandleUiLanguageChangedAsync(CultureInfo cultureInfo)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            _logger.LogTrace("Changing UI language to {CultureInfo}...", cultureInfo);

            await Task.Run(
                    async () =>
                    {
                        _cultureManager.ChangeCulture(cultureInfo);

                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                        foreach (var translation in _translationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                        {
                            translation.ReRenderWord();
                        }

                        _semaphore.Release();
                    },
                    CancellationTokenSource.Token)
                .ConfigureAwait(true);
        }

        async Task OpenDetailsAsync(TranslationEntryViewModel translationEntryViewModel)
        {
            _ = translationEntryViewModel ?? throw new ArgumentNullException(nameof(translationEntryViewModel));
            _logger.LogTrace("Opening details for {Translation}...", translationEntryViewModel);

            var translationEntry = _translationEntryRepository.GetById(translationEntryViewModel.Id);
            var translationDetails = await TranslationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationTokenSource.Token)
                .ConfigureAwait(false);
            var learningInfo = _learningInfoRepository.GetOrInsert(translationEntry.Id);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails, learningInfo);
            var ownerWindow = await _dictionaryWindowFactory.GetWindowAsync(CancellationTokenSource.Token).ConfigureAwait(false);
            var window = await _scopedWindowProvider.GetScopedWindowAsync<ITranslationDetailsCardWindow, (IDisplayable, TranslationInfo)>((ownerWindow, translationInfo), CancellationTokenSource.Token)
                .ConfigureAwait(false);
            _synchronizationContext.Send(
                _ =>
                {
                    _windowPositionAdjustmentManager.AdjustActivatedWindow(window);
                    window.Restore();
                },
                null);
        }

        async Task OpenSettingsAsync()
        {
            _logger.LogTrace("Opening settings...");

            await _settingsWindowFactory.ShowWindowAsync(CancellationTokenSource.Token).ConfigureAwait(false);
        }

        void ScrollTo(TranslationEntryViewModel translationEntryViewModel)
        {
            _synchronizationContext.Post(
                async _ =>
                {
                    if (View.CurrentItem == translationEntryViewModel)
                    {
                        await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(true);

                        if (_translationList.Count > 0)
                        {
                            View.MoveCurrentTo(_translationList[0]);
                        }

                        _semaphore.Release();
                    }

                    View.MoveCurrentTo(translationEntryViewModel);
                },
                null);
        }

        void Search(string? text)
        {
            _logger.LogTrace("Searching for {Text}...", text);

            View.Filter = string.IsNullOrWhiteSpace(text)
#pragma warning disable IDE0004 // Remove Unnecessary Cast
                ? (Predicate<object>?)null
#pragma warning restore IDE0004 // Remove Unnecessary Cast
                : obj =>
                {
                    var translationEntryViewModel = (TranslationEntryViewModel)obj;
                    return string.IsNullOrWhiteSpace(text) ||
                           translationEntryViewModel.Id.Text.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                           translationEntryViewModel.Translations.Any(translation => translation.Word.Text.Contains(text, StringComparison.OrdinalIgnoreCase));
                };

            _filterChanged = true;

            // Count = View.Cast<object>().Count();
        }

        async void Timer_TickAsync(object? state)
        {
            await _semaphore.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(true);
            foreach (var translation in _translationList)
            {
                var time = translation.LearningInfoViewModel.NextCardShowTime;
                if (time > DateTimeOffset.Now)
                {
                    translation.LearningInfoViewModel.ReRenderNextCardShowTime();
                }
            }

            _semaphore.Release();
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1312:Variable names should begin with lower-case letter", Justification = "Discarded variable")]
        void TranslationList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (TranslationEntryViewModel translationEntryViewModel in e.OldItems)
                        {
                            Interlocked.Decrement(ref _count);
                            TranslationEntryProcessor.DeleteTranslationEntry(translationEntryViewModel.Id);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (var _ in e.NewItems)
                        {
                            Interlocked.Increment(ref _count);
                        }
                    }

                    break;
            }

            if (_loaded)
            {
                _rateLimiter.ThrottleAsync(TimeSpan.FromSeconds(1), UpdateCount).ConfigureAwait(true);
            }
        }

        void UpdateCount()
        {
            if (!_filterChanged && (_count == _lastRecordedCount))
            {
                return;
            }

            _filterChanged = false;
            _lastRecordedCount = _count;

            Count = View.Filter == null ? _count : View.Cast<object>().Count();
        }

        async Task WindowContentRenderedAsync()
        {
            await LoadTranslationsAsync().ConfigureAwait(false);
        }

        async Task<bool> ConfirmDeletionAsync(string name)
        {
            var confirmationViewModel = _confirmationViewModelFactory(string.Format(CultureInfo.InvariantCulture, Texts.AreYouSureDelete, name), true);

            _synchronizationContext.Send(
                _ =>
                {
                    var confirmationWindow = _confirmationWindowFactory(confirmationViewModel);
                    confirmationWindow.ShowDialog();
                },
                null);
            return await confirmationViewModel.UserInput.ConfigureAwait(false);
        }
    }
}
