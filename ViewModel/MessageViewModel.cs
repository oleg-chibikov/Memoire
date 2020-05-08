using System;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class MessageViewModel : BaseViewModel
    {
        public MessageViewModel(Message message, ILogger<MessageViewModel> logger, ICommandManager commandManager) : base(commandManager)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            AutoCloseTimeout = AppSettings.MessageCloseTimeout;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            logger.LogTrace("Showing message {0} and closing window in {1}...", message, AutoCloseTimeout);
        }

        public TimeSpan AutoCloseTimeout { get; }

        public Message Message { get; }
    }
}
