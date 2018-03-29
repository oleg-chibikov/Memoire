using System;
using System.Threading;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.ViewModel.Translation;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IAssessmentInfoProvider assessmentInfoProvider,
            [NotNull] IPauseManager pauseManager)
            : base(translationInfo, messageHub, logger, wordViewModelFactory, synchronizationContext, assessmentInfoProvider, pauseManager)
        {
            if (translationDetailsCardViewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            }

            var translationDetailsCardViewModel = translationDetailsCardViewModelFactory(translationInfo);
            TranslationDetailsCardViewModel = translationDetailsCardViewModel ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));

            Logger.Trace("Showing view only card...");
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            learningInfoRepository.Update(learningInfo);
            Logger.InfoFormat("Increased repeat type for {0}", learningInfo);
            MessageHub.Publish(learningInfo);
        }

        [NotNull]
        public TranslationDetailsCardViewModel TranslationDetailsCardViewModel { get; }
    }
}