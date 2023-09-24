using System;
using Easy.MessageHub;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
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
        readonly Action _loadDetailsViewModel;
        bool _isExpanded;

        public AssessmentViewOnlyCardViewModel(
            ILearningInfoRepository learningInfoRepository,
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
            _ = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = translationDetailsCardViewModelFactory ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));
            var translationDetailsCardViewModel = translationDetailsCardViewModelFactory(translationInfo);
            _loadDetailsViewModel = () => TranslationDetailsCardViewModel = translationDetailsCardViewModel ?? throw new ArgumentNullException(nameof(translationDetailsCardViewModelFactory));

            // Learning info will be saved and published by the caller
            var learningInfo = TranslationInfo.LearningInfo;
            learningInfo.IncreaseRepeatType();
            learningInfoRepository.Update(learningInfo);
            logger.LogInformation("Increased repeat type for {LearningInfo}", learningInfo);
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public event EventHandler? IsExpandedChanged;

        public TranslationDetailsCardViewModel? TranslationDetailsCardViewModel { get; private set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                IsExpandedChanged?.Invoke(this, EventArgs.Empty);
                if (value && TranslationDetailsCardViewModel == null)
                {
                    _loadDetailsViewModel();
                }
            }
        }
    }
}
