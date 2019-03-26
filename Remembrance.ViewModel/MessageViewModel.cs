using System;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Resources;
using Scar.Common.Messages;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class MessageViewModel : BaseViewModel
    {
        public MessageViewModel([NotNull] Message message, [NotNull] ILog logger, [NotNull] ICommandManager commandManager)
            : base(commandManager)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            AutoCloseTimeout = AppSettings.MessageCloseTimeout;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            logger.TraceFormat("Showing message {0} and closing window in {1}...", message, AutoCloseTimeout);
        }

        public TimeSpan AutoCloseTimeout { get; }

        [NotNull]
        public Message Message { get; }
    }
}