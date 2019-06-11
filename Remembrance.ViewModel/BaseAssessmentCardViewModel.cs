using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Common.Logging;
using Easy.MessageHub;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;

namespace Remembrance.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseAssessmentCardViewModel : BaseViewModel
    {
        private readonly ICultureManager _cultureManager;

        private readonly IPauseManager _pauseManager;

        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        protected readonly HashSet<Word> AcceptedAnswers;

        protected readonly ILog Logger;

        protected readonly IMessageHub MessageHub;

        protected readonly TranslationInfo TranslationInfo;

        protected readonly Func<Word, string, WordViewModel> WordViewModelFactory;

        protected BaseAssessmentCardViewModel(
            TranslationInfo translationInfo,
            IMessageHub messageHub,
            ILog logger,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            IAssessmentInfoProvider assessmentInfoProvider,
            IPauseManager pauseManager,
            Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ICultureManager cultureManager,
            ICommandManager commandManager)
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

        public WordViewModel CorrectAnswer { get; protected set; }

        public string LanguagePair { get; }

        public LearningInfoViewModel LearningInfoViewModel { get; }

        public string? Tooltip { get; }

        public ICommand WindowClosedCommand { get; }

        public WordViewModel Word { get; }

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

        private static bool HasUppercaseLettersExceptFirst(string text)
        {
            return text.Substring(1).Any(letter => char.IsLetter(letter) && char.IsUpper(letter));
        }

        private static void PrepareVerb(string sourceLanguage, BaseWord word)
        {
            if (sourceLanguage != Constants.EnLanguageTwoLetters || word.PartOfSpeech != PartOfSpeech.Verb)
            {
                return;
            }

            word.Text = "To " + (HasUppercaseLettersExceptFirst(word.Text) ? word.Text : word.Text.ToLowerInvariant());
        }

        private async void OnLearningInfoReceivedAsync(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            if (!learningInfo.Id.Equals(TranslationInfo.TranslationEntryKey))
            {
                return;
            }

            Logger.DebugFormat("Received {0} from external source", learningInfo);

            await Task.Run(() => LearningInfoViewModel.UpdateLearningInfo(learningInfo), CancellationToken.None).ConfigureAwait(false);
        }

        private async void OnUiLanguageChangedAsync(CultureInfo cultureInfo)
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