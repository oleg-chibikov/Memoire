using System;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] IAssessmentInfoProvider assessmentInfoProvider,
            [NotNull] IPauseManager pauseManager,
            [NotNull] Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            [NotNull] Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory)
            : base(translationInfo, messageHub, logger, wordViewModelFactory, assessmentInfoProvider, pauseManager, wordImageViewerViewModelFactory, learningInfoViewModelFactory)
        {
            if (translationDetailsCardViewModelFactory == null)
            {
                throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            }

            var translationDetailsCardViewModel = translationDetailsCardViewModelFactory(translationInfo);
            TranslationDetailsCardViewModel = translationDetailsCardViewModel ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));

            Logger.Trace("Showing view only card...");

            // Learning info will be saved and published by the caller
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            Logger.InfoFormat("Increased repeat type for {0}", learningInfo);
        }

        [NotNull]
        public TranslationDetailsCardViewModel TranslationDetailsCardViewModel { get; }
    }
}