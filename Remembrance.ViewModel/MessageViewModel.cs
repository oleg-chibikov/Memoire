using System;
using Common.Logging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class MessageViewModel
    {
        public MessageViewModel([NotNull] Message message, [NotNull] ILog logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            AutoCloseTimeout = AppSettings.MessageCloseTimeout;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            logger.TraceFormat("Showing message {0} and closing window in {1}...", message, AutoCloseTimeout);
        }

        public TimeSpan AutoCloseTimeout { get; }

        [NotNull]
        public Message Message { get; }
    }
}