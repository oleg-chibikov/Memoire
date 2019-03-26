using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.ProcessMonitoring;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Scar.Common.MVVM.CollectionView;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class ProcessBlacklistViewModel : BaseViewModel
    {
        [NotNull]
        private readonly IActiveProcessesProvider _activeProcessesProvider;

        [NotNull]
        private readonly ObservableCollection<ProcessInfo> _availableProcesses = new ObservableCollection<ProcessInfo>();

        [NotNull]
        private readonly ILog _logger;

        [CanBeNull]
        private string? _filter;

        public ProcessBlacklistViewModel(
            [NotNull] IActiveProcessesProvider activeProcessesProvider,
            [NotNull] ILog logger,
            [NotNull] ICommandManager commandManager,
            [NotNull] ICollectionViewSource collectionViewSource)
            : base(commandManager)
        {
            _activeProcessesProvider = activeProcessesProvider ?? throw new ArgumentNullException(nameof(activeProcessesProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpenProcessesListCommand = AddCommand(OpenProcessesList);
            AddFromActiveProcessesCommand = AddCommand<IList>(AddFromActiveProcesses);
            AddTextCommand = AddCommand(AddText);
            CancelAdditionCommand = AddCommand(CancelAddition);
            DeleteCommand = AddCommand<ProcessInfo>(Delete);
            DeleteBatchCommand = AddCommand<IList>(DeleteBatch);
            ClearFilterCommand = AddCommand(ClearFilter);
            AvailableProcessesView = collectionViewSource.GetDefaultView(_availableProcesses);
            BlacklistedProcessesView = collectionViewSource.GetDefaultView(BlacklistedProcesses);
            AvailableProcessesView.Filter = o => string.IsNullOrWhiteSpace(Filter) || ((ProcessInfo)o).Name.IndexOf(Filter, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        [NotNull]
        public ICommand AddFromActiveProcessesCommand { get; }

        [NotNull]
        public ICommand AddTextCommand { get; }

        [NotNull]
        public ICollectionView AvailableProcessesView { get; }

        [NotNull]
        public ObservableCollection<ProcessInfo> BlacklistedProcesses { get; } = new ObservableCollection<ProcessInfo>();

        [NotNull]
        public ICollectionView BlacklistedProcessesView { get; }

        [NotNull]
        public ICommand CancelAdditionCommand { get; }

        [NotNull]
        public ICommand ClearFilterCommand { get; }

        [NotNull]
        public ICommand DeleteBatchCommand { get; }

        [NotNull]
        public ICommand DeleteCommand { get; }

        [CanBeNull]
        public string? Filter
        {
            get => _filter;
            set
            {
                _filter = value;
                AvailableProcessesView.Refresh();
            }
        }

        public bool IsActiveProcessesDialogOpen { get; private set; }

        [NotNull]
        public ICommand OpenProcessesListCommand { get; }

        [CanBeNull]
        public string? Text { get; set; }

        private void AddFromActiveProcesses([NotNull] IList processesList)
        {
            _ = processesList ?? throw new ArgumentNullException(nameof(processesList));
            var processInfos = processesList.Cast<ProcessInfo>();

            foreach (var processInfo in processInfos)
            {
                AddProcessInfo(processInfo);
            }

            IsActiveProcessesDialogOpen = false;
            Filter = null;
        }

        private void AddProcessInfo(ProcessInfo processInfo)
        {
            _logger.TraceFormat("Adding process info {0} to the blacklist...", processInfo);

            if (!BlacklistedProcesses.Contains(processInfo))
            {
                BlacklistedProcesses.Add(processInfo);
                _logger.InfoFormat("Process info {0} is added to the blacklist...", processInfo);
            }
            else
            {
                _logger.DebugFormat("Process info {0} is already in the blacklist...", processInfo);
            }
        }

        private void AddText()
        {
            if (string.IsNullOrWhiteSpace(Text))
            {
                return;
            }

            AddProcessInfo(new ProcessInfo(Text));
            Text = null;
        }

        private void CancelAddition()
        {
            IsActiveProcessesDialogOpen = false;
            Filter = null;
        }

        private void ClearFilter()
        {
            Filter = null;
        }

        private void Delete([NotNull] ProcessInfo processInfo)
        {
            _ = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            _logger.TraceFormat("Deleting process info {0} from the blacklist...", processInfo);

            BlacklistedProcesses.Remove(processInfo);
        }

        private void DeleteBatch([NotNull] IList processesList)
        {
            _ = processesList ?? throw new ArgumentNullException(nameof(processesList));
            _logger.Trace("Deleting multiple process infos from the blacklist...");

            var processInfos = processesList.Cast<ProcessInfo>().ToArray();

            foreach (var processInfo in processInfos)
            {
                BlacklistedProcesses.Remove(processInfo);
            }
        }

        private void OpenProcessesList()
        {
            _availableProcesses.Clear();
            var activeProcesses = _activeProcessesProvider.GetActiveProcesses();
            foreach (var activeProcess in activeProcesses.Where(processInfo => !BlacklistedProcesses.Contains(processInfo)))
            {
                _availableProcesses.Add(activeProcess);
            }

            IsActiveProcessesDialogOpen = true;
        }
    }
}