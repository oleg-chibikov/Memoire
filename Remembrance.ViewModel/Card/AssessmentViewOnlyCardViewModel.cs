using System.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] SynchronizationContext synchronizationContext)
            : base(translationInfo, settingsRepository, messageHub, logger, lifetimeScope, synchronizationContext)
        {
            Logger.Trace("Showing view-only card...");
            TranslationInfo.LearningInfo.IncreaseRepeatType();
            learningInfoRepository.Update(TranslationInfo.LearningInfo);
            Logger.InfoFormat("Increased repeat type for {0}", TranslationInfo);
            MessageHub.Publish(TranslationInfo.TranslationEntry);
        }
    }
}