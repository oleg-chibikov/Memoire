using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
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
            [NotNull] IEqualityComparer<IWord> wordsEqualityComparer,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IWordsProcessor wordsProcessor)
            : base(translationInfo, settingsRepository, viewModelAdapter, messenger, wordsEqualityComparer, logger, lifetimeScope)
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
            logger.Trace("Card is initialized");
        }

        [NotNull]
        public ICommand ProvideAnswerCommand { get; }

        [CanBeNull]
        public bool? Accepted { get; private set; }

        [CanBeNull]
        public string ProvidedAnswer { get; set; }

        private void ProvideAnswer()
        {
            Logger.Info($"Providing answer {ProvidedAnswer}...");
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
                Logger.Info($"Answer is correct. Most suitable accepted word was {mostSuitable} with distance {currentMinDistance}. Increasing repeat type for {TranslationInfo}...");
                TranslationInfo.TranslationEntry.IncreaseRepeatType();

                // The inputed answer can differ from the first one
                CorrectAnswer = mostSuitable;
                closeTimeout = SuccessCloseTimeout;
            }
            else
            {
                Logger.Info($"Answer is not correct. Decreasing repeat type for {TranslationInfo}...");
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