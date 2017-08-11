using System;
using System.Threading;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;
using Scar.Common;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class TranslationResultCardManager : BaseCardManager, ITranslationResultCardManager
    {
        // TODO: config timeout
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        public TranslationResultCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
            : base(lifetimeScope, settingsRepository, logger)
        {
        }

        [NotNull]
        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            Logger.Trace($"Creating window for {translationInfo}...");
            var translationResultCardViewModel = LifetimeScope.Resolve<TranslationResultCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var translationResultCardWindow = LifetimeScope.Resolve<ITranslationResultCardWindow>(
                new TypedParameter(typeof(TranslationResultCardViewModel), translationResultCardViewModel),
                new TypedParameter(typeof(Window), ownerWindow));
            translationResultCardWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.TopRight;

            Logger.Trace($"Closing window in {CloseTimeout}...");
            ActionExtensions.DoAfterAsync(
                () =>
                {
                    _syncContext.Post(x => translationResultCardWindow.Close(), null);
                    Logger.Trace("Window is closed");
                },
                CloseTimeout);
            return translationResultCardWindow;
        }
    }
}