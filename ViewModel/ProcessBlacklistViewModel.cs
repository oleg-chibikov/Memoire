using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.ProcessMonitoring;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.CollectionView;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class ProcessBlacklistViewModel : BaseViewModel, IWithDeleteCommand
    {
        readonly IActiveProcessesProvider _activeProcessesProvider;
        readonly ILogger _logger;
        string? _filter;

        public ProcessBlacklistViewModel(
            IActiveProcessesProvider activeProcessesProvider,
            ILogger<ProcessBlacklistViewModel> logger,
            ICommandManager commandManager,
            ICollectionViewSource collectionViewSource) : base(commandManager)
        {
            _ = collectionViewSource ?? throw new ArgumentNullException(nameof(collectionViewSource));
            _activeProcessesProvider = activeProcessesProvider ?? throw new ArgumentNullException(nameof(activeProcessesProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            OpenProcessesListCommand = AddCommand(OpenProcessesList);
            AddFromActiveProcessesCommand = AddCommand<IList>(AddFromActiveProcesses);
            AddTextCommand = AddCommand(AddText);
            CancelAdditionCommand = AddCommand(CancelAddition);
            DeleteCommand = AddCommand<ProcessInfo>(Delete);
            DeleteBatchCommand = AddCommand<IList>(DeleteBatch);
            ClearFilterCommand = AddCommand(ClearFilter);
            AvailableProcessesView = collectionViewSource.GetDefaultView(AvailableProcesses);
            AvailableProcessesView.Filter = o => string.IsNullOrWhiteSpace(Filter) || (((ProcessInfo)o).Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public ICommand AddFromActiveProcessesCommand { get; }

        public ICommand AddTextCommand { get; }

        public ObservableCollection<ProcessInfo> AvailableProcesses { get; } = new ObservableCollection<ProcessInfo>();

        public ICollectionView AvailableProcessesView { get; }

        public ObservableCollection<ProcessInfo> BlacklistedProcesses { get; } = new ObservableCollection<ProcessInfo>();

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

        void AddFromActiveProcesses(IList processesList)
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

        void AddProcessInfo(ProcessInfo processInfo)
        {
            _logger.LogTrace("Adding process info {0} to the blacklist...", processInfo);

            if (!BlacklistedProcesses.Contains(processInfo))
            {
                BlacklistedProcesses.Add(processInfo);
                _logger.LogInformation("Process info {0} is added to the blacklist...", processInfo);
            }
            else
            {
                _logger.LogDebug("Process info {0} is already in the blacklist...", processInfo);
            }
        }

        void AddText()
        {
            if ((Text == null) || string.IsNullOrWhiteSpace(Text))
            {
                return;
            }

            AddProcessInfo(new ProcessInfo(Text));
            Text = null;
        }

        void CancelAddition()
        {
            IsActiveProcessesDialogOpen = false;
            Filter = null;
        }

        void ClearFilter()
        {
            Filter = null;
        }

        void Delete(ProcessInfo processInfo)
        {
            _ = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            _logger.LogTrace("Deleting process info {0} from the blacklist...", processInfo);

            BlacklistedProcesses.Remove(processInfo);
        }

        void DeleteBatch(IList processesList)
        {
            _ = processesList ?? throw new ArgumentNullException(nameof(processesList));
            _logger.LogTrace("Deleting multiple process infos from the blacklist...");

            var processInfos = processesList.Cast<ProcessInfo>().ToArray();

            foreach (var processInfo in processInfos)
            {
                BlacklistedProcesses.Remove(processInfo);
            }
        }

        void OpenProcessesList()
        {
            AvailableProcesses.Clear();
            var activeProcesses = _activeProcessesProvider.GetActiveProcesses();
            foreach (var activeProcess in activeProcesses.Where(processInfo => !BlacklistedProcesses.Contains(processInfo)))
            {
                AvailableProcesses.Add(activeProcess);
            }

            IsActiveProcessesDialogOpen = true;
        }
    }
}
