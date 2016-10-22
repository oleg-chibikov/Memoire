using System;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Card.View.Contracts;
using Remembrance.Card.ViewModel.Contracts;
using Remembrance.DAL.Contracts;
using Remembrance.DAL.Contracts.Model;
using Scar.Common;
using Scar.Common.WPF;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal class TranslationResultCardManager : BaseCardManager, ITranslationResultCardManager
    {
        public TranslationResultCardManager(
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger) : base(lifetimeScope, settingsRepository, logger)
        {
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo)
        {
            var translationResultCardViewModel = LifetimeScope.Resolve<ITranslationResultCardViewModel>(
                new TypedParameter(typeof(TranslationInfo), translationInfo)
            );
            var translationDetailsWindow = LifetimeScope.Resolve<ITranslationResultCardWindow>(
                new TypedParameter(typeof(ITranslationResultCardViewModel), translationResultCardViewModel));
            ActionExtensions.DoAfter(() => translationDetailsWindow.Close(), TimeSpan.FromSeconds(5));
            return translationDetailsWindow;
        }
    }
}