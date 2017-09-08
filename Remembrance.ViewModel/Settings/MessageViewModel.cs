using System;
using Common.Logging;
using JetBrains.Annotations;
using Scar.Common.Messages;

namespace Remembrance.ViewModel.Settings
{
    [UsedImplicitly]
    public sealed class MessageViewModel
    {
        //TODO: Configure
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(3);

        public MessageViewModel([NotNull] Message message, [NotNull] ILog logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            Message = message ?? throw new ArgumentNullException(nameof(message));
            logger.Trace($"Showing message {message} and closing window in {CloseTimeout}...");
        }

        [NotNull]
        public Message Message { get; }
    }
}