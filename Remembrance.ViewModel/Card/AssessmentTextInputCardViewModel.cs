using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.ViewModel.Translation;
using Scar.Common;
using Scar.Common.WPF.Commands;

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentTextInputCardViewModel : BaseAssessmentCardViewModel
    {
        [NotNull]
        private readonly ILearningInfoRepository _learningInfoRepository;

        public AssessmentTextInputCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(translationInfo, settingsRepository, messageHub, logger, lifetimeScope, synchronizationContext)
        {
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));

            ProvideAnswerCommand = new CorrelationCommand(ProvideAnswer);
            logger.DebugFormat("Card for {0} is initialized", translationInfo);
        }

        [NotNull]
        public ICommand ProvideAnswerCommand { get; }

        [CanBeNull]
        public bool? Accepted { get; private set; }

        [CanBeNull]
        public string ProvidedAnswer { get; set; }

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

        private TimeSpan ChangeRepeatType(WordViewModel mostSuitable, int currentMinDistance)
        {
            TimeSpan closeTimeout;
            var learningInfo = _learningInfoRepository.GetById(TranslationInfo.TranslationEntryKey);

            if (Accepted == true)
            {
                Logger.InfoFormat("Answer is correct. Most suitable accepted word was {0} with distance {1}. Increasing repeat type for {2}...", mostSuitable, currentMinDistance, learningInfo);
                learningInfo.IncreaseRepeatType();

                // The inputed answer can differ from the first one
                CorrectAnswer = mostSuitable;
                closeTimeout = SuccessCloseTimeout;
            }
            else
            {
                Logger.InfoFormat("Answer is not correct. Decreasing repeat type for {0}...", learningInfo);
                Accepted = false;
                learningInfo.DecreaseRepeatType();
                closeTimeout = FailureCloseTimeout;
            }

            _learningInfoRepository.Update(learningInfo);
            MessageHub.Publish(TranslationInfo.TranslationEntry);
            return closeTimeout;
        }
    }
}