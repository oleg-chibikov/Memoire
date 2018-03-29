// TODO: Feature: if the word level is low, replace textbox with dropdown
// TODO: Display image near to assessmentCard

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.ViewModel.Card
{
    // TODO: Test delete file then save or smth
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseAssessmentCardViewModel : IRequestCloseViewModel, IDisposable
    {
        [NotNull]
        protected readonly HashSet<Word> AcceptedAnswers;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly IMessageHub MessageHub;

        [NotNull]
        protected readonly TranslationInfo TranslationInfo;

        [NotNull]
        protected readonly Func<Word, string, WordViewModel> WordViewModelFactory;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        protected BaseAssessmentCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] IAssessmentInfoProvider assessmentInfoProvider,
            [NotNull] IPauseManager pauseManager)
        {
            if (assessmentInfoProvider == null)
            {
                throw new ArgumentNullException(nameof(assessmentInfoProvider));
            }

            MessageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            TranslationInfo = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            WordViewModelFactory = wordViewModelFactory ?? throw new ArgumentNullException(nameof(wordViewModelFactory));

            var assessmentInfo = assessmentInfoProvider.ProvideAssessmentInfo(translationInfo);
            var sourceLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.TargetLanguage : TranslationInfo.TranslationEntryKey.SourceLanguage;
            var targetLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.SourceLanguage : TranslationInfo.TranslationEntryKey.TargetLanguage;
            LanguagePair = $"{sourceLanguage} -> {targetLanguage}";
            RepeatType = translationInfo.LearningInfo.RepeatType;
            AcceptedAnswers = assessmentInfo.AcceptedAnswers;
            var word = wordViewModelFactory(assessmentInfo.Word, sourceLanguage);
            if (sourceLanguage == Constants.EnLanguageTwoLetters && word.PartOfSpeech == PartOfSpeech.Verb)
            {
                word.Text = "to " + word.Text;
            }

            word.CanLearnWord = false;
            Word = word;
            CorrectAnswer = wordViewModelFactory(assessmentInfo.CorrectAnswer, targetLanguage);
            if (CorrectAnswer.Text.Length > 2)
            {
                Tooltip = CorrectAnswer.Text[0] + string.Join(string.Empty, Enumerable.Range(0, CorrectAnswer.Text.Length - 2).Select(x => '*')) + CorrectAnswer.Text[CorrectAnswer.Text.Length - 1];
            }

            pauseManager.Pause(PauseReason.CardIsVisible, word.ToString());

            WindowClosedCommand = new CorrelationCommand(WindowClosed);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
        }

        public event EventHandler RequestClose;

        [NotNull]
        public WordViewModel CorrectAnswer { get; protected set; }

        [NotNull]
        public string LanguagePair { get; }

        public RepeatType RepeatType { get; }

        [CanBeNull]
        public string Tooltip { get; }

        [NotNull]
        public ICommand WindowClosedCommand { get; }

        [NotNull]
        public WordViewModel Word { get; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                MessageHub.UnSubscribe(subscriptionToken);
            }

            _subscriptionTokens.Clear();
        }

        protected void CloseWindowWithTimeout(TimeSpan closeTimeout)
        {
            Logger.TraceFormat("Closing window in {0}...", closeTimeout);
            ActionExtensions.DoAfterAsync(
                () =>
                {
                    Logger.Trace("Window is closing...");
                    _synchronizationContext.Post(x => RequestClose?.Invoke(null, null), null);
                },
                closeTimeout);
        }

        private async void OnUiLanguageChangedAsync([NotNull] CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentNullException(nameof(cultureInfo));
            }

            Logger.TraceFormat("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    () =>
                    {
                        CultureUtilities.ChangeCulture(cultureInfo);
                        Word.ReRender();
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        private void WindowClosed()
        {
            _pauseManager.Resume(PauseReason.CardIsVisible);
        }
    }
}