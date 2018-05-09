using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Remembrance.Resources;
using Scar.Common.WPF.View;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsCardManager : BaseCardManager, ITranslationDetailsCardManager
    {
        [NotNull]
        private readonly IScopedWindowProvider _scopedWindowProvider;

        public TranslationDetailsCardManager(
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IScopedWindowProvider scopedWindowProvider)
            : base(localSettingsRepository, logger, synchronizationContext)
        {
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
        }

        [ItemNotNull]
        protected override async Task<IWindow> TryCreateWindowAsync(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            Logger.TraceFormat("Creating window for {0}...", translationInfo);

            // ReSharper disable once StyleCop.SA1009
            var translationDetailsCardWindow = await _scopedWindowProvider
                .GetScopedWindowAsync<ITranslationDetailsCardWindow, (IWindow, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                .ConfigureAwait(false);
            SynchronizationContext.Send(
                x =>
                {
                    translationDetailsCardWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.TopRight;
                    translationDetailsCardWindow.AutoCloseTimeout = AppSettings.TranslationCardCloseTimeout;
                },
                null);
            return translationDetailsCardWindow;
        }
    }
}