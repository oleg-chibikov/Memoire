using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
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
        private readonly ITranslationEntryRepository _translationEntryRepository;

        public AssessmentTextInputCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] SynchronizationContext synchronizationContext)
            : base(translationInfo, settingsRepository, viewModelAdapter, messenger, logger, lifetimeScope, synchronizationContext)
        {
            if (settingsRepository == null)
            {
                throw new ArgumentNullException(nameof(settingsRepository));
            }

            if (viewModelAdapter == null)
            {
                throw new ArgumentNullException(nameof(viewModelAdapter));
            }

            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));

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

            if (Accepted == true)
            {
                Logger.InfoFormat("Answer is correct. Most suitable accepted word was {0} with distance {1}. Increasing repeat type for {2}...", mostSuitable, currentMinDistance, TranslationInfo);
                TranslationInfo.TranslationEntry.IncreaseRepeatType();

                // The inputed answer can differ from the first one
                CorrectAnswer = mostSuitable;
                closeTimeout = SuccessCloseTimeout;
            }
            else
            {
                Logger.InfoFormat("Answer is not correct. Decreasing repeat type for {0}...", TranslationInfo);
                Accepted = false;
                TranslationInfo.TranslationEntry.DecreaseRepeatType();
                closeTimeout = FailureCloseTimeout;
            }

            _translationEntryRepository.Update(TranslationInfo.TranslationEntry);
            Messenger.Publish(TranslationInfo);
            return closeTimeout;
        }
    }
}