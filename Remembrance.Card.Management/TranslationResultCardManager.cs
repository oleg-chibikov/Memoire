using System;
using System.Threading;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Scar.Common;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class TranslationResultCardManager : BaseCardManager, ITranslationResultCardManager
    {
        //TODO: config timeout
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        public TranslationResultCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
            : base(lifetimeScope, settingsRepository, logger)
        {
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            Logger.Trace($"Creating window for {translationInfo}...");
            var translationResultCardViewModel = LifetimeScope.Resolve<ITranslationResultCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var translationDetailsWindow = LifetimeScope.Resolve<ITranslationResultCardWindow>(
                new TypedParameter(typeof(ITranslationResultCardViewModel), translationResultCardViewModel),
                new TypedParameter(typeof(Window), ownerWindow));

            Logger.Trace($"Closing window in {CloseTimeout}...");
            ActionExtensions.DoAfterAsync(
                () =>
                {
                    _syncContext.Post(x => translationDetailsWindow.Close(), null);
                    Logger.Trace("Window is closed");
                },
                CloseTimeout);
            return translationDetailsWindow;
        }
    }
}