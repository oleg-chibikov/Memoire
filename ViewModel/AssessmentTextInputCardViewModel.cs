using System;
using System.Linq;
using System.Windows.Input;
using Easy.MessageHub;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentTextInputCardViewModel : BaseAssessmentCardViewModel, IFocusableViewModel
    {
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly ILogger _logger;

        public AssessmentTextInputCardViewModel(
            TranslationInfo translationInfo,
            AssessmentBatchCardViewModel assessmentBatchCardViewModel,
            IMessageHub messageHub,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            ILearningInfoRepository learningInfoRepository,
            IAssessmentInfoProvider assessmentInfoProvider,
            Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ICultureManager cultureManager,
            ICommandManager commandManager,
            ILogger<AssessmentTextInputCardViewModel> logger,
            ILogger<BaseAssessmentCardViewModel> baseLogger) : base(
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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));

            ProvideAnswerCommand = AddCommand(ProvideAnswer);
            logger.LogDebug($"Initialized {GetType().Name}");
        }

        public bool? Accepted { get; private set; }

        public ICommand ProvideAnswerCommand { get; }

        public string? ProvidedAnswer { get; set; }

        TimeSpan ChangeRepeatType(Word mostSuitable, int currentMinDistance)
        {
            TimeSpan closeTimeout;
            var learningInfo = _learningInfoRepository.GetById(TranslationInfo.TranslationEntryKey);

            if (Accepted == true)
            {
                _logger.LogInformation("Answer is correct. Most suitable accepted word was {0} with distance {1}. Increasing repeat type for {2}...", mostSuitable, currentMinDistance, learningInfo);
                learningInfo.IncreaseRepeatType();

                // The inputted answer can differ from the first one
                CorrectAnswer = WordViewModelFactory(mostSuitable, TranslationInfo.TranslationEntryKey.TargetLanguage);
                closeTimeout = AppSettings.AssessmentCardSuccessCloseTimeout;
            }
            else
            {
                _logger.LogInformation("Answer is not correct. Decreasing repeat type for {0}...", learningInfo);
                Accepted = false;
                learningInfo.DecreaseRepeatType();
                closeTimeout = AppSettings.AssessmentCardFailureCloseTimeout;
            }

            _learningInfoRepository.Update(learningInfo);
            MessageHub.Publish(learningInfo);
            return closeTimeout;
        }

        void ProvideAnswer()
        {
            _logger.LogTrace("Providing answer {0}...", ProvidedAnswer);
            var mostSuitable = AcceptedAnswers.First();
            var currentMinDistance = int.MaxValue;
            if (!string.IsNullOrWhiteSpace(ProvidedAnswer))
            {
                foreach (var acceptedAnswer in AcceptedAnswers)
                {
                    // 20% of the word could be errors
                    var maxDistance = acceptedAnswer.Text.Length / 5;
                    var distance = ProvidedAnswer.LevenshteinDistance(acceptedAnswer.Text);
                    if ((distance < 0) || (distance > maxDistance))
                    {
                        continue;
                    }

                    if (distance < currentMinDistance)
                    {
                        currentMinDistance = distance;
                        mostSuitable = acceptedAnswer;
                    }

                    Accepted = true;
                }
            }

            var closeTimeout = ChangeRepeatType(mostSuitable, currentMinDistance);
            _ = HideControlWithTimeoutAsync(closeTimeout);
        }
    }
}
