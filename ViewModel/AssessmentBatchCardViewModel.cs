using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentBatchCardViewModel : BaseViewModel
    {
        readonly IAssessmentCardManager _assessmentCardManager;
        int _notAnsweredCount;

        public AssessmentBatchCardViewModel(
            ILogger<AssessmentBatchCardViewModel> logger,
            ISharedSettingsRepository sharedSettingsRepository,
            ICommandManager commandManager,
            IReadOnlyCollection<TranslationInfo> translationInfos,
            Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentTextInputCardViewModel> assessmentTextInputCardViewModelFactory,
            Func<TranslationInfo, AssessmentBatchCardViewModel, AssessmentViewOnlyCardViewModel> assessmentViewOnlyCardViewModelFactory,
            IAssessmentCardManager assessmentCardManager) : base(commandManager)
        {
            _ = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _ = assessmentCardManager ?? throw new ArgumentNullException(nameof(assessmentCardManager));
            _ = assessmentTextInputCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentTextInputCardViewModelFactory));
            _ = assessmentViewOnlyCardViewModelFactory ?? throw new ArgumentNullException(nameof(assessmentViewOnlyCardViewModelFactory));
            _assessmentCardManager = assessmentCardManager;
            _ = translationInfos ?? throw new ArgumentNullException(nameof(translationInfos));

            Title = string.Join(", ", translationInfos.Select(x => x.TranslationEntryKey.ToString()));

            WindowClosedCommand = AddCommand(WindowClosed);
            WindowContentRenderedCommand = AddCommand(WindowContentRendered);
            NestedViewModels = translationInfos.Select(
                    x =>
                    {
                        if (!ShouldUserInputTextForTranslation(x.LearningInfo.RepeatType))
                        {
                            return (BaseAssessmentCardViewModel)assessmentViewOnlyCardViewModelFactory(x, this);
                        }
                        else
                        {
                            _notAnsweredCount++;
                            return assessmentTextInputCardViewModelFactory(x, this);
                        }
                    })
                .ToArray();
            NestedViewModels.First().IsFocused = true;
            Opacity = sharedSettingsRepository.CardWindowOpacity;
            NestedViewModels.OfType<AssessmentViewOnlyCardViewModel>().ForEach(assessmentViewOnlyCardViewModel =>
            {
                assessmentViewOnlyCardViewModel.IsExpandedChanged += AssessmentViewOnlyCardViewModel_IsExpandedChanged;
            });
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public bool IsExpanded { get; set; }

        public double Opacity { get; }

        public IReadOnlyCollection<BaseAssessmentCardViewModel> NestedViewModels { get; }

        public string Title { get; }

        public ICommand WindowClosedCommand { get; }

        public ICommand WindowContentRenderedCommand { get; }

        public void NotifyChildClosed()
        {
            if (--_notAnsweredCount == 0)
            {
                CloseWindow();
            }
        }

        public void NotifyChildIsClosing()
        {
            var firstOtherChild = NestedViewModels.OfType<IFocusableViewModel>().FirstOrDefault(x => !x.IsFocused && x is { IsHidden: false, IsHiding: false });
            if (firstOtherChild != null)
            {
                firstOtherChild.IsFocused = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                NestedViewModels.OfType<AssessmentViewOnlyCardViewModel>().ForEach(assessmentViewOnlyCardViewModel =>
                {
                    assessmentViewOnlyCardViewModel.IsExpandedChanged -=
                        AssessmentViewOnlyCardViewModel_IsExpandedChanged;
                });
            }
        }

        static bool ShouldUserInputTextForTranslation(RepeatType repeatType)
        {
            switch (repeatType)
            {
                case RepeatType.Elementary:
                case RepeatType.Beginner:
                case RepeatType.Novice:
                    {
                        return false;
                    }

                case RepeatType.PreIntermediate:
                case RepeatType.Intermediate:
                case RepeatType.UpperIntermediate:
                    {
                        return false;
                    }

                case RepeatType.Advanced:
                case RepeatType.Proficiency:
                case RepeatType.Expert:
                    {
                        return true;
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatType));
            }
        }

        void AssessmentViewOnlyCardViewModel_IsExpandedChanged(object? sender, EventArgs e)
        {
            IsExpanded = NestedViewModels.OfType<AssessmentViewOnlyCardViewModel>().Any(x => x.IsExpanded);
        }

        void WindowContentRendered()
        {
            _assessmentCardManager.Pause(Title);
        }

        void WindowClosed()
        {
            // Setting the card show time as when the card was closed.
            _assessmentCardManager.ResetInterval();
        }
    }
}
