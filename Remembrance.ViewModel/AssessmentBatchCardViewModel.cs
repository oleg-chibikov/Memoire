using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using PropertyChanged;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentBatchCardViewModel : BaseViewModel
    {
        readonly Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;
        readonly Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentViewOnlyCardViewModel> _assessmentViewOnlyCardViewModelFactory;
        readonly IPauseManager _pauseManager;
        int _closedCount;

        public AssessmentBatchCardViewModel(
            ILog logger,
            ICommandManager commandManager,
            IReadOnlyCollection<TranslationInfo> translationInfos,
            Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentTextInputCardViewModel> assessmentTextInputCardViewModelFactory,
            Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentViewOnlyCardViewModel> assessmentViewOnlyCardViewModelFactory,
            IPauseManager pauseManager) : base(commandManager)
        {
            _assessmentTextInputCardViewModelFactory = assessmentTextInputCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardViewModelFactory));
            _assessmentViewOnlyCardViewModelFactory = assessmentViewOnlyCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardViewModelFactory));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            _ = translationInfos ?? throw new ArgumentNullException(nameof(translationInfos));

            logger.Trace("Showing batch assessment window...");

            Title = string.Join(", ", translationInfos.Select(x => x.TranslationEntryKey.ToString()));
            pauseManager.PauseActivity(PauseReasons.CardIsVisible, Title);
            WindowClosedCommand = AddCommand(WindowClosed);
            NestedViewModels = translationInfos.Select(x => GetViewModelByRepeatType(x, x.LearningInfo.RepeatType));
            _closedCount = translationInfos.Count;
        }

        public IEnumerable<BaseAssessmentCardViewModel> NestedViewModels { get; }

        public string Title { get; }

        public ICommand WindowClosedCommand { get; }

        public void NotifyChildClosed()
        {
            if (--_closedCount == 0)
            {
                CloseWindow();
            }
        }

        BaseAssessmentCardViewModel GetViewModelByRepeatType(TranslationInfo x, RepeatType repeatType)
        {
            switch (repeatType)
            {
                case RepeatType.Elementary:
                case RepeatType.Beginner:
                case RepeatType.Novice:
                    {
                        return _assessmentViewOnlyCardViewModelFactory(x, this);
                    }

                case RepeatType.PreIntermediate:
                case RepeatType.Intermediate:
                case RepeatType.UpperIntermediate:
                    {
                        return _assessmentTextInputCardViewModelFactory(x, this);
                    }

                case RepeatType.Advanced:
                case RepeatType.Proficiency:
                case RepeatType.Expert:
                    {
                        return _assessmentTextInputCardViewModelFactory(x, this);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatType));
            }
        }

        void WindowClosed()
        {
            _pauseManager.ResumeActivity(PauseReasons.CardIsVisible);
        }
    }
}
