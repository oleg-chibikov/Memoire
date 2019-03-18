using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PropertyChanged;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    [UsedImplicitly]
    public sealed class ConfirmationViewModel : IRequestCloseViewModel
    {
        [NotNull]
        public string Text { get; }

        public bool ShowButtons { get; }

        private readonly TaskCompletionSource<bool> _taskCompletionSource;

        [DoNotNotify]
        public Task<bool> UserInput => _taskCompletionSource.Task;

        public ConfirmationViewModel(bool showButtons, [NotNull] string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            DeclineCommand = new CorrelationCommand(Decline);
            ConfirmCommand = new CorrelationCommand(Confirm);
            WindowClosedCommand = new CorrelationCommand(Decline);
            _taskCompletionSource = new TaskCompletionSource<bool>();
            ShowButtons = showButtons;
        }

        [NotNull]
        public IRefreshableCommand DeclineCommand { get; }

        [NotNull]
        public IRefreshableCommand ConfirmCommand { get; }

        [NotNull]
        public IRefreshableCommand WindowClosedCommand { get; }

        private void Decline()
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                return;
            }

            _taskCompletionSource.SetResult(ShowButtons == false);
            RequestClose?.Invoke(this, new EventArgs());
        }

        private void Confirm()
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                return;
            }

            _taskCompletionSource.SetResult(true);
            RequestClose?.Invoke(this, new EventArgs());
        }

        public event EventHandler RequestClose;
    }
}