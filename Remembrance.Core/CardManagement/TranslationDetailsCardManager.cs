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
    internal sealed class TranslationDetailsCardManager : BaseCardManager, ITranslationDetailsCardManager
    {
        public TranslationDetailsCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ISettingsRepository settingsRepository, [NotNull] ILog logger)
            : base(lifetimeScope, settingsRepository, logger)
        {
        }

        [NotNull]
        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            Logger.Trace($"Creating window for {translationInfo}...");
            var translationDetailsCardViewModel = LifetimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var translationDetailsCardWindow = LifetimeScope.Resolve<ITranslationDetailsCardWindow>(
                new TypedParameter(typeof(TranslationDetailsCardViewModel), translationDetailsCardViewModel),
                new TypedParameter(typeof(Window), ownerWindow));
            translationDetailsCardWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.TopRight;
            translationDetailsCardWindow.AutoCloseTimeout = SettingsRepository.Get().TranslationCloseTimeout;
            return translationDetailsCardWindow;
        }
    }
}