using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using PropertyChanged;
using Remembrance.Contracts.ProcessMonitoring;
using Remembrance.Contracts.ProcessMonitoring.Data;
using Scar.Common.MVVM.CollectionView;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class ProcessBlacklistViewModel : BaseViewModel
    {
        private readonly IActiveProcessesProvider _activeProcessesProvider;

        private readonly ObservableCollection<ProcessInfo> _availableProcesses = new ObservableCollection<ProcessInfo>();

        private readonly ILog _logger;

        private string? _filter;

        public ProcessBlacklistViewModel(IActiveProcessesProvider activeProcessesProvider, ILog logger, ICommandManager commandManager, ICollectionViewSource collectionViewSource)
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

        public ICommand AddFromActiveProcessesCommand { get; }

        public ICommand AddTextCommand { get; }

        public ICollectionView AvailableProcessesView { get; }

        public ObservableCollection<ProcessInfo> BlacklistedProcesses { get; } = new ObservableCollection<ProcessInfo>();

        public ICollectionView BlacklistedProcessesView { get; }

        public ICommand CancelAdditionCommand { get; }

        public ICommand ClearFilterCommand { get; }

        public ICommand DeleteBatchCommand { get; }

        public ICommand DeleteCommand { get; }

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

        public ICommand OpenProcessesListCommand { get; }

        public string? Text { get; set; }

        private void AddFromActiveProcesses(IList processesList)
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
            if (Text == null || string.IsNullOrWhiteSpace(Text))
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

        private void Delete(ProcessInfo processInfo)
        {
            _ = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            _logger.TraceFormat("Deleting process info {0} from the blacklist...", processInfo);

            BlacklistedProcesses.Remove(processInfo);
        }

        private void DeleteBatch(IList processesList)
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