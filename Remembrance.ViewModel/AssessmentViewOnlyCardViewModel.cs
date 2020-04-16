using System;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            TranslationInfo translationInfo,
            IMessageHub messageHub,
            ILog logger,
            Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            IAssessmentInfoProvider assessmentInfoProvider,
            IPauseManager pauseManager,
            Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ICultureManager cultureManager,
            ICommandManager commandManager) : base(
            translationInfo,
            messageHub,
            logger,
            wordViewModelFactory,
            assessmentInfoProvider,
            pauseManager,
            wordImageViewerViewModelFactory,
            learningInfoViewModelFactory,
            cultureManager,
            commandManager)
        {
            _ = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            var translationDetailsCardViewModel = translationDetailsCardViewModelFactory(translationInfo);
            TranslationDetailsCardViewModel = translationDetailsCardViewModel ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));

            Logger.Trace("Showing view only card...");

            // Learning info will be saved and published by the caller
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            Logger.InfoFormat("Increased repeat type for {0}", learningInfo);
        }

        public TranslationDetailsCardViewModel TranslationDetailsCardViewModel { get; }
    }
}
