using System;
using System.Windows;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class TranslationDetailsCardManager : BaseCardManager, ITranslationDetailsCardManager
    {
        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        public TranslationDetailsCardManager([NotNull] ILifetimeScope lifetimeScope, [NotNull] ILocalSettingsRepository localSettingsRepository, [NotNull] ILog logger, [NotNull] ISettingsRepository settingsRepository)
            : base(lifetimeScope, localSettingsRepository, logger)
        {
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        }

        [NotNull]
        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            Logger.Trace($"Creating window for {translationInfo}...");
            var nestedLifeTimeScope = LifetimeScope.BeginLifetimeScope();
            var translationDetailsCardViewModel = nestedLifeTimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
            var translationDetailsCardWindow = nestedLifeTimeScope.Resolve<ITranslationDetailsCardWindow>(
                new TypedParameter(typeof(TranslationDetailsCardViewModel), translationDetailsCardViewModel),
                new TypedParameter(typeof(Window), ownerWindow));
            translationDetailsCardWindow.AdvancedWindowStartupLocation = AdvancedWindowStartupLocation.TopRight;
            translationDetailsCardWindow.AutoCloseTimeout = _settingsRepository.Get().TranslationCloseTimeout;
            translationDetailsCardWindow.AssociateDisposable(nestedLifeTimeScope);
            return translationDetailsCardWindow;
        }
    }
}