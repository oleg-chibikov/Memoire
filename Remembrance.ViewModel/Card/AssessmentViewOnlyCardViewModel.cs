using System;
using System.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] SynchronizationContext synchronizationContext)
            : base(translationInfo, settingsRepository, viewModelAdapter, messenger, logger, lifetimeScope, synchronizationContext)
        {
            if (settingsRepository == null)
            {
                throw new ArgumentNullException(nameof(settingsRepository));
            }

            if (viewModelAdapter == null)
            {
                throw new ArgumentNullException(nameof(viewModelAdapter));
            }

            Logger.Trace("Showing view-only card...");
            TranslationInfo.TranslationEntry.IncreaseRepeatType();
            translationEntryRepository.Update(TranslationInfo.TranslationEntry);
            Logger.InfoFormat("Increased repeat type for {0}", TranslationInfo);
            Messenger.Publish(TranslationInfo);
        }
    }
}