using System;
using System.Linq;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common;
using Scar.Common.MVVM.Commands;

namespace Remembrance.ViewModel
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentTextInputCardViewModel : BaseAssessmentCardViewModel
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        public AssessmentTextInputCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] IAssessmentInfoProvider assessmentInfoProvider,
            [NotNull] IPauseManager pauseManager,
            [NotNull] Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            [NotNull] Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            [NotNull] ICultureManager cultureManager,
            [NotNull] ICommandManager commandManager)
            : base(
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
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));

            Logger.Trace("Showing text input card...");

            ProvideAnswerCommand = AddCommand(ProvideAnswer);
        }

        [CanBeNull]
        public bool? Accepted { get; private set; }

        [NotNull]
        public ICommand ProvideAnswerCommand { get; }

        [CanBeNull]
        public string? ProvidedAnswer { get; set; }

        private TimeSpan ChangeRepeatType(Word mostSuitable, int currentMinDistance)
        {
            TimeSpan closeTimeout;
            var learningInfo = _learningInfoRepository.GetById(TranslationInfo.TranslationEntryKey);

            if (Accepted == true)
            {
                Logger.InfoFormat(
                    "Answer is correct. Most suitable accepted word was {0} with distance {1}. Increasing repeat type for {2}...",
                    mostSuitable,
                    currentMinDistance,
                    learningInfo);
                learningInfo.IncreaseRepeatType();

                // The inputed answer can differ from the first one
                CorrectAnswer = WordViewModelFactory(mostSuitable, TranslationInfo.TranslationEntryKey.TargetLanguage);
                closeTimeout = AppSettings.AssessmentCardSuccessCloseTimeout;
            }
            else
            {
                Logger.InfoFormat("Answer is not correct. Decreasing repeat type for {0}...", learningInfo);
                Accepted = false;
                learningInfo.DecreaseRepeatType();
                closeTimeout = AppSettings.AssessmentCardFailureCloseTimeout;
            }

            _learningInfoRepository.Update(learningInfo);
            MessageHub.Publish(learningInfo);
            return closeTimeout;
        }

        private void ProvideAnswer()
        {
            Logger.TraceFormat("Providing answer {0}...", ProvidedAnswer);
            var mostSuitable = AcceptedAnswers.First();
            var currentMinDistance = int.MaxValue;
            if (!string.IsNullOrWhiteSpace(ProvidedAnswer))
            {
                foreach (var acceptedAnswer in AcceptedAnswers)
                {
                    // 20% of the word could be errors
                    var maxDistance = acceptedAnswer.Text.Length / 5;
                    var distance = ProvidedAnswer.LevenshteinDistance(acceptedAnswer.Text);
                    if (distance < 0 || distance > maxDistance)
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
            CloseWindowWithTimeout(closeTimeout);
        }
    }
}