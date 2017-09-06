using System;
using Common.Logging;
using JetBrains.Annotations;

namespace Remembrance.ViewModel.Settings
{
    public enum MessageType
    {
        Message,
        Warning,
        Error
    }

    [UsedImplicitly]
    public sealed class MessageViewModel
    {
        //TODO: Configure
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(3);

        public MessageViewModel([NotNull] string message, MessageType messageType, [NotNull] ILog logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            MessageType = messageType;
            logger.Trace($"Showing message {message} and closing window in {CloseTimeout}...");
        }

        [NotNull]
        public string Message { get; }

        public MessageType MessageType { get; }
    }
}