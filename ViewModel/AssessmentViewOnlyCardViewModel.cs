using System;
using Easy.MessageHub;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentViewOnlyCardViewModel : BaseAssessmentCardViewModel
    {
        public AssessmentViewOnlyCardViewModel(
            TranslationInfo translationInfo,
            AssessmentBatchCardViewModel assessmentBatchCardViewModel,
            IMessageHub messageHub,
            ILogger<AssessmentTextInputCardViewModel> logger,
            ILogger<BaseAssessmentCardViewModel> baseLogger,
            Func<TranslationInfo, TranslationDetailsCardViewModel> translationDetailsCardViewModelFactory,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            IAssessmentInfoProvider assessmentInfoProvider,
            Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ICultureManager cultureManager,
            ICommandManager commandManager) : base(
            translationInfo,
            messageHub,
            baseLogger,
            wordViewModelFactory,
            assessmentInfoProvider,
            wordImageViewerViewModelFactory,
            learningInfoViewModelFactory,
            cultureManager,
            commandManager,
            assessmentBatchCardViewModel)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            var translationDetailsCardViewModel = translationDetailsCardViewModelFactory(translationInfo);
            TranslationDetailsCardViewModel = translationDetailsCardViewModel ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));

            logger.LogTrace("Showing view only card...");

            // Learning info will be saved and published by the caller
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            logger.LogInformation("Increased repeat type for {0}", learningInfo);
        }

        public TranslationDetailsCardViewModel TranslationDetailsCardViewModel { get; }
    }
}
