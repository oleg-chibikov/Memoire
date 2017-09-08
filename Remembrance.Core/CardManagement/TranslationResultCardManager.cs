using System;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class TranslationResultCardManager : BaseCardManager, ITranslationResultCardManager
    {
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
            translationResultCardWindow.AutoCloseTimeout = SettingsRepository.Get().TranslationCloseTimeout;
            return translationResultCardWindow;
        }
    }
}