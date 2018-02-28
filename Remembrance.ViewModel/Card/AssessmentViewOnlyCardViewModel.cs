using System.Threading;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;

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
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            learningInfoRepository.Update(learningInfo);
            Logger.InfoFormat("Increased repeat type for {0}", learningInfo);
            MessageHub.Publish(learningInfo);
        }
    }
}