using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autofac;
using CCSWE.Collections.ObjectModel;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.View.Card;
using Remembrance.Contracts.View.Settings;
using Remembrance.Resources;
using Remembrance.ViewModel.Card;
using Remembrance.ViewModel.Translation;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class DictionaryViewModel : BaseViewModelWithAddTranslationControl, IDisposable
    {
        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        [NotNull]
        private readonly SynchronizedObservableCollection<TranslationEntryViewModel> _translationList;

        public DictionaryViewModel(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ITranslationDetailsRepository translationDetailsRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILanguageDetector languageDetector,
            [NotNull] IWordsProcessor wordsProcessor,
            [NotNull] ILog logger,
            [NotNull] ViewModelAdapter viewModelAdapter,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessenger messenger,
            [NotNull] WindowFactory<IDictionaryWindow> dictionaryWindowFactory,
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer)
            : base(settingsRepository, languageDetector, wordsProcessor, logger)
        {
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));
            _dictionaryWindowFactory = dictionaryWindowFactory ?? throw new ArgumentNullException(nameof(dictionaryWindowFactory));

            _wordsEqualityComparer = wordsEqualityComparer ?? throw new ArgumentNullException(nameof(wordsEqualityComparer));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _viewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
            _translationDetailsRepository = translationDetailsRepository ?? throw new ArgumentNullException(nameof(translationDetailsRepository));

            DeleteCommand = new CorrelationCommand<TranslationEntryViewModel>(Delete);
            OpenDetailsCommand = new CorrelationCommand<TranslationEntryViewModel>(OpenDetails);
            OpenSettingsCommand = new CorrelationCommand(OpenSettings);
            SearchCommand = new CorrelationCommand<string>(Search);

            Logger.Info("Starting...");

            Logger.Trace("Receiving translations...");
            //TODO: Load asynchronously
            //TODO: Load by pages (scroll down asks new page)
            var translationEntryViewModels = viewModelAdapter.Adapt<TranslationEntryViewModel[]>(translationEntryRepository.GetAll());
            foreach (var translationEntryViewModel in translationEntryViewModels)
                translationEntryViewModel.TextChanged += TranslationEntryViewModel_TextChanged;

            //Need to create this list before subscribing the events
            _translationList = new SynchronizedObservableCollection<TranslationEntryViewModel>(translationEntryViewModels);
            Count = translationEntryViewModels.Length;
            Logger.Trace("Translations have been received");

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

            messenger.Register<TranslationInfo>(this, MessengerTokens.TranslationInfoToken, OnWordReceived);
            messenger.Register<TranslationInfo[]>(this, MessengerTokens.TranslationInfoBatchToken, OnWordsBatchReceived);
            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, OnUiLanguageChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityChangeToken, OnPriorityChanged);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityAddToken, OnPriorityAdded);
            messenger.Register<PriorityWordViewModel>(this, MessengerTokens.PriorityRemoveToken, OnPriorityRemoved);

            Logger.Info("Started");
        }

        protected override IWindow Window => _dictionaryWindowFactory.GetWindowIfExists();

        public ICollectionView View { get; }

        public void Dispose()
        {
            Logger.Trace("Disposing...");
            _translationList.CollectionChanged -= TranslationList_CollectionChanged;
            _timer.Tick -= Timer_Tick;
            _timer.Stop();
            Logger.Trace("Disposed");
        }

        #region Dependencies

        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        [NotNull]
        private readonly DispatcherTimer _timer;

        [NotNull]
        private readonly ITranslationDetailsRepository _translationDetailsRepository;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly ViewModelAdapter _viewModelAdapter;

        [NotNull]
        private readonly WindowFactory<IDictionaryWindow> _dictionaryWindowFactory;

        [NotNull]
        private readonly IEqualityComparer<IWord> _wordsEqualityComparer;

        #endregion

        #region EventHandlers

        private void OnWordsBatchReceived([NotNull] TranslationInfo[] translationInfos)
        {
            Logger.Trace($"Received a batch of translations ({translationInfos.Length} items) from external source...");
            foreach (var translationInfo in translationInfos)
                OnWordReceived(translationInfo);
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
                //Prevent text change to fire
                using (existing.SupressNotification())
                {
                    _viewModelAdapter.Adapt(translationInfo.TranslationEntry, existing);
                }
                //TODO: Sync context instead of dispather
                _syncContext.Post(x => View.MoveCurrentTo(existing), null);
                Logger.Trace($"{existing} has been updated in the list");
            }
            else
            {
                Logger.Trace($"Adding {translationEntryViewModel} to the list...");
                _syncContext.Post(
                    x =>
                    {
                        _translationList.Add(translationEntryViewModel);
                        View.MoveCurrentToLast();
                    },
                    null);
                translationEntryViewModel.TextChanged += TranslationEntryViewModel_TextChanged;
                Logger.Trace($"{translationEntryViewModel} has been added to the list...");
            }
        }

        private void OnPriorityChanged([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            Logger.Trace($"Changing priority for {priorityWordViewModel} in the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.TranslationEntryId;
            var changed = false;
            var translationEntryViewModel = _translationList.SingleOrDefault(x => Equals(x.Id, parentId));
            var translation = translationEntryViewModel?.Translations.SingleOrDefault(x => _wordsEqualityComparer.Equals(x, priorityWordViewModel));
            if (translation != null)
            {
                translation.IsPriority = priorityWordViewModel.IsPriority;
                changed = true;
            }
            if (changed)
                Logger.Trace($"Changed priority for {priorityWordViewModel}");
            else
                Logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnPriorityAdded([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            Logger.Trace($"Adding {priorityWordViewModel} to the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.TranslationEntryId;
            var translationEntryViewModel = _translationList.SingleOrDefault(x => Equals(x.Id, parentId));
            translationEntryViewModel?.Translations.Add(priorityWordViewModel);
            if (translationEntryViewModel != null)
                Logger.Trace($"Added {priorityWordViewModel} to {translationEntryViewModel}");
            else
                Logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnPriorityRemoved([NotNull] PriorityWordViewModel priorityWordViewModel)
        {
            Logger.Trace($"Removing {priorityWordViewModel} from the list...");
            if (priorityWordViewModel == null)
                throw new ArgumentNullException(nameof(priorityWordViewModel));

            var parentId = priorityWordViewModel.TranslationEntryId;
            var removed = false;
            var translationEntryViewModel = _translationList.SingleOrDefault(x => Equals(x.Id, parentId));
            var correlated = translationEntryViewModel?.Translations.SingleOrDefault(x => _wordsEqualityComparer.Equals(x, priorityWordViewModel));
            if (correlated != null)
                removed = translationEntryViewModel.Translations.Remove(correlated);
            if (removed)
                Logger.Trace($"Removed {priorityWordViewModel} from the list");
            else
                Logger.Warn($"There is no matching translation for {priorityWordViewModel} in the list");
        }

        private void OnUiLanguageChanged([NotNull] string uiLanguage)
        {
            Logger.Trace($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage);

            foreach (var translation in _translationList.SelectMany(translationEntryViewModel => translationEntryViewModel.Translations))
                translation.ReRender();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            foreach (var translation in _translationList)
            {
                var time = translation.NextCardShowTime;
                translation.NextCardShowTime = time.AddTicks(1); //To launch converter
            }
        }

        private bool TranslationEntryViewModel_TextChanged([NotNull] object sender, [NotNull] TextChangedEventArgs e)
        {
            var translationEntryViewModel = (TranslationEntryViewModel) sender;
            Logger.Info($"Changing translation's text for {translationEntryViewModel} to {e.NewValue}...");

            var sourceLanguage = translationEntryViewModel.Language;
            var targetLanguage = translationEntryViewModel.TargetLanguage;
            return e.NewValue != null && WordsProcessor.ChangeWord(translationEntryViewModel.Id, e.NewValue, sourceLanguage, targetLanguage, Window);
        }

        private void TranslationList_CollectionChanged([NotNull] object sender, [NotNull] NotifyCollectionChangedEventArgs e)
        {
            Count = View.Cast<object>().Count();
            if (e.Action != NotifyCollectionChangedAction.Remove)
                return;

            foreach (TranslationEntryViewModel translationEntryViewModel in e.OldItems)
            {
                _translationDetailsRepository.DeleteByTranslationEntryId(translationEntryViewModel.Id);
                _translationEntryRepository.Delete(translationEntryViewModel.Id);
            }
        }

        #endregion

        #region Dependency properties

        public int Count { get; private set; }

        public string SearchText { get; set; }

        #endregion

        #region Commands

        public ICommand DeleteCommand { get; }

        public ICommand OpenDetailsCommand { get; }

        public ICommand OpenSettingsCommand { get; }

        public ICommand SearchCommand { get; }

        #endregion

        #region Command handlers

        private void Delete([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            //TODO: prompt
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

        private void OpenDetails([NotNull] TranslationEntryViewModel translationEntryViewModel)
        {
            Logger.Trace($"Opening details for {translationEntryViewModel}...");
            if (translationEntryViewModel == null)
                throw new ArgumentNullException(nameof(translationEntryViewModel));

            var translationEntry = _viewModelAdapter.Adapt<TranslationEntry>(translationEntryViewModel);
            var translationInfo = WordsProcessor.ReloadTranslationDetailsIfNeeded(translationEntry);
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

        private void Search([CanBeNull] string text)
        {
            Logger.Trace($"Searching for {text}...");
            View.Filter = o => string.IsNullOrWhiteSpace(text) || ((TranslationEntryViewModel) o).Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) >= 0;
            Count = View.Cast<object>().Count();
        }

        #endregion
    }
}