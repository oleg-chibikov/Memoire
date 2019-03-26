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
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseAssessmentCardViewModel : BaseViewModel
    {
        [NotNull]
        private readonly ICultureManager _cultureManager;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

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

        protected BaseAssessmentCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] IMessageHub messageHub,
            [NotNull] ILog logger,
            [NotNull] Func<Word, string, WordViewModel> wordViewModelFactory,
            [NotNull] IAssessmentInfoProvider assessmentInfoProvider,
            [NotNull] IPauseManager pauseManager,
            [NotNull] Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            [NotNull] Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            [NotNull] ICultureManager cultureManager,
            [NotNull] ICommandManager commandManager)
            : base(commandManager)
        {
            _ = assessmentInfoProvider ?? throw new ArgumentNullException(nameof(assessmentInfoProvider));
            _ = learningInfoViewModelFactory ?? throw new ArgumentNullException(nameof(learningInfoViewModelFactory));
            MessageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            TranslationInfo = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
            WordViewModelFactory = wordViewModelFactory ?? throw new ArgumentNullException(nameof(wordViewModelFactory));
            LearningInfoViewModel = learningInfoViewModelFactory(translationInfo.LearningInfo);

            var assessmentInfo = assessmentInfoProvider.ProvideAssessmentInfo(translationInfo);
            var sourceLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.TargetLanguage : TranslationInfo.TranslationEntryKey.SourceLanguage;
            var targetLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.SourceLanguage : TranslationInfo.TranslationEntryKey.TargetLanguage;
            LanguagePair = $"{sourceLanguage} -> {targetLanguage}";
            AcceptedAnswers = assessmentInfo.AcceptedAnswers;
            var wordViewModel = wordViewModelFactory(assessmentInfo.Word, sourceLanguage);
            PrepareVerb(sourceLanguage, wordViewModel.Word);

            wordViewModel.CanLearnWord = false;
            Word = wordViewModel;
            CorrectAnswer = wordViewModelFactory(assessmentInfo.CorrectAnswer, targetLanguage);
            if (CorrectAnswer.Word.Text.Length > 2)
            {
                var text = CorrectAnswer.Word.Text;
                Tooltip = text[0] + string.Join(string.Empty, Enumerable.Range(0, text.Length - 2).Select(x => '*')) + text[text.Length - 1];
            }

            WordImageViewerViewModel = wordImageViewerViewModelFactory(
                new WordKey(translationInfo.TranslationEntryKey, assessmentInfo.CorrectAnswer),
                assessmentInfo.Word.Text,
                true);

            pauseManager.Pause(PauseReason.CardIsVisible, wordViewModel.ToString());

            WindowClosedCommand = AddCommand(WindowClosed);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<LearningInfo>(OnLearningInfoReceivedAsync));
        }

        [NotNull]
        public WordViewModel CorrectAnswer { get; protected set; }

        [NotNull]
        public string LanguagePair { get; }

        [NotNull]
        public LearningInfoViewModel LearningInfoViewModel { get; }

        [CanBeNull]
        public string? Tooltip { get; }

        [NotNull]
        public ICommand WindowClosedCommand { get; }

        [NotNull]
        public WordViewModel Word { get; }

        [NotNull]
        public WordImageViewerViewModel WordImageViewerViewModel { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                foreach (var subscriptionToken in _subscriptionTokens)
                {
                    MessageHub.Unsubscribe(subscriptionToken);
                }

                _subscriptionTokens.Clear();
            }
        }

        protected async void CloseWindowWithTimeout(TimeSpan closeTimeout)
        {
            Logger.TraceFormat("Closing window in {0}...", closeTimeout);
            await Task.Delay(closeTimeout).ConfigureAwait(true);
            CloseWindow();
            Logger.Debug("Window is closed");
        }

        private static bool HasUppercaseLettersExceptFirst([NotNull] string text)
        {
            return text.Substring(1).Any(letter => char.IsLetter(letter) && char.IsUpper(letter));
        }

        private static void PrepareVerb([NotNull] string sourceLanguage, [NotNull] BaseWord word)
        {
            if (sourceLanguage != Constants.EnLanguageTwoLetters || word.PartOfSpeech != PartOfSpeech.Verb)
            {
                return;
            }

            word.Text = "To " + (HasUppercaseLettersExceptFirst(word.Text) ? word.Text : word.Text.ToLowerInvariant());
        }

        private async void OnLearningInfoReceivedAsync([NotNull] LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            if (!learningInfo.Id.Equals(TranslationInfo.TranslationEntryKey))
            {
                return;
            }

            Logger.DebugFormat("Received {0} from external source", learningInfo);

            await Task.Run(() => LearningInfoViewModel.UpdateLearningInfo(learningInfo), CancellationToken.None).ConfigureAwait(false);
        }

        private async void OnUiLanguageChangedAsync([NotNull] CultureInfo cultureInfo)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            Logger.TraceFormat("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    () =>
                    {
                        _cultureManager.ChangeCulture(cultureInfo);
                        Word.ReRenderWord();
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