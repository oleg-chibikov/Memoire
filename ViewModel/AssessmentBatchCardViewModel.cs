using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentBatchCardViewModel : BaseViewModel
    {
        readonly Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentTextInputCardViewModel> _assessmentTextInputCardViewModelFactory;
        readonly Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentViewOnlyCardViewModel> _assessmentViewOnlyCardViewModelFactory;
        readonly IPauseManager _pauseManager;
        int _closedCount;

        public AssessmentBatchCardViewModel(
            ILogger<AssessmentBatchCardViewModel> logger,
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

            logger.LogTrace("Showing batch assessment window...");

            Title = string.Join(", ", translationInfos.Select(x => x.TranslationEntryKey.ToString()));

            WindowClosedCommand = AddCommand(WindowClosed);
            WindowContentRenderedCommand = AddCommand(WindowContentRendered);
            NestedViewModels = translationInfos.Select(x => GetViewModelByRepeatType(x, x.LearningInfo.RepeatType)).ToArray();
            NestedViewModels.First().IsFocused = true;
            _closedCount = translationInfos.Count;
        }

        public IReadOnlyCollection<BaseAssessmentCardViewModel> NestedViewModels { get; }

        public string Title { get; }

        public ICommand WindowClosedCommand { get; }

        public ICommand WindowContentRenderedCommand { get; }

        public void NotifyChildClosed()
        {
            if (--_closedCount == 0)
            {
                CloseWindow();
            }
        }

        public void NotifyChildIsClosing()
        {
            var firstOtherChild = NestedViewModels.OfType<IFocusableViewModel>().FirstOrDefault(x => !x.IsFocused && !x.IsHidden && !x.IsHiding);
            if (firstOtherChild != null)
            {
                firstOtherChild.IsFocused = true;
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

        void WindowContentRendered()
        {
            _pauseManager.PauseActivity(PauseReasons.CardIsVisible, Title);
        }

        void WindowClosed()
        {
            _pauseManager.ResumeActivity(PauseReasons.CardIsVisible);
        }
    }
}
