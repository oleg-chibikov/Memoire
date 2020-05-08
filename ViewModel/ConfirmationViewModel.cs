using System;
using System.Threading.Tasks;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class ConfirmationViewModel : BaseViewModel
    {
        readonly TaskCompletionSource<bool> _taskCompletionSource;

        public ConfirmationViewModel(bool showButtons, string text, ICommandManager commandManager) : base(commandManager)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            DeclineCommand = AddCommand(Decline);
            ConfirmCommand = AddCommand(Confirm);
            WindowClosedCommand = AddCommand(Decline);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            ShowButtons = showButtons;
        }

        public string Text { get; }

        public bool ShowButtons { get; }

        [DoNotNotify]
        public Task<bool> UserInput => _taskCompletionSource.Task;

        public IRefreshableCommand DeclineCommand { get; }

        public IRefreshableCommand ConfirmCommand { get; }

        public IRefreshableCommand WindowClosedCommand { get; }

        void Decline()
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                return;
            }

            _taskCompletionSource.SetResult(ShowButtons == false);
            CloseWindow();
        }

        void Confirm()
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                return;
            }

            _taskCompletionSource.SetResult(true);
            CloseWindow();
        }
    }
}
