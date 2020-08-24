using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.Processing.Data;
using Microsoft.Extensions.Logging;
using PropertyChanged;
using Scar.Common.Localization;
using Scar.Common.MVVM.Commands;
using Scar.Common.MVVM.ViewModel;
using Scar.Services.Contracts.Data;
using Scar.Services.Contracts.Data.Translation;

namespace Mémoire.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseAssessmentCardViewModel : BaseViewModel
    {
        readonly ICultureManager _cultureManager;
        readonly IList<Guid> _subscriptionTokens = new List<Guid>();
        readonly AssessmentBatchCardViewModel _assessmentBatchCardViewModel;
        readonly ILogger _logger;

        protected BaseAssessmentCardViewModel(
            TranslationInfo translationInfo,
            IMessageHub messageHub,
            ILogger<BaseAssessmentCardViewModel> logger,
            Func<Word, string, WordViewModel> wordViewModelFactory,
            IAssessmentInfoProvider assessmentInfoProvider,
            Func<WordKey, string, bool, WordImageViewerViewModel> wordImageViewerViewModelFactory,
            Func<LearningInfo, LearningInfoViewModel> learningInfoViewModelFactory,
            ICultureManager cultureManager,
            ICommandManager commandManager,
            AssessmentBatchCardViewModel assessmentBatchCardViewModel) : base(commandManager)
        {
            _ = wordImageViewerViewModelFactory ?? throw new ArgumentNullException(nameof(wordImageViewerViewModelFactory));
            _ = assessmentInfoProvider ?? throw new ArgumentNullException(nameof(assessmentInfoProvider));
            _ = learningInfoViewModelFactory ?? throw new ArgumentNullException(nameof(learningInfoViewModelFactory));
            MessageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            TranslationInfo = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cultureManager = cultureManager ?? throw new ArgumentNullException(nameof(cultureManager));
            _assessmentBatchCardViewModel = assessmentBatchCardViewModel ?? throw new ArgumentNullException(nameof(assessmentBatchCardViewModel));
            WordViewModelFactory = wordViewModelFactory ?? throw new ArgumentNullException(nameof(wordViewModelFactory));
            LearningInfoViewModel = learningInfoViewModelFactory(translationInfo.LearningInfo);

            var assessmentInfo = assessmentInfoProvider.ProvideAssessmentInfo(translationInfo);
            var sourceLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.TargetLanguage : TranslationInfo.TranslationEntryKey.SourceLanguage;
            var targetLanguage = assessmentInfo.IsReverse ? TranslationInfo.TranslationEntryKey.SourceLanguage : TranslationInfo.TranslationEntryKey.TargetLanguage;
            LanguagePair = $"{sourceLanguage} -> {targetLanguage}";
            AcceptedAnswers = assessmentInfo.AcceptedAnswers;
            var wordViewModel = wordViewModelFactory(assessmentInfo.Word, sourceLanguage);
            PrepareVerb(sourceLanguage, wordViewModel.Word);
            SourceLanguageSynonyms = assessmentInfo.Synonyms.Select(
                    x =>
                    {
                        // No need to show part of speech
                        x.PartOfSpeech = PartOfSpeech.Unknown;
                        return x;
                    })
                .Select(x => wordViewModelFactory(x, sourceLanguage));
            wordViewModel.CanLearnWord = false;
            Word = wordViewModel;
            CorrectAnswer = wordViewModelFactory(assessmentInfo.CorrectAnswer, targetLanguage);
            if (CorrectAnswer.Word.Text.Length > 2)
            {
                var text = CorrectAnswer.Word.Text;
                Tooltip = text[0] + string.Join(string.Empty, Enumerable.Range(0, text.Length - 2).Select(x => '*')) + text[^1];
            }

            WordImageViewerViewModel = wordImageViewerViewModelFactory(new WordKey(translationInfo.TranslationEntryKey, assessmentInfo.CorrectAnswer), assessmentInfo.Word.Text, true);

            _subscriptionTokens.Add(messageHub.Subscribe<CultureInfo>(HandleUiLanguageChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<LearningInfo>(HandleLearningInfoReceivedAsync));
        }

        public bool IsFocused { get; set; }

        public IEnumerable<WordViewModel> SourceLanguageSynonyms { get; }

        public WordViewModel CorrectAnswer { get; protected set; }

        public bool IsHiding { get; private set; }

        public bool IsHidden { get; private set; }

        [DependsOn(nameof(IsHidden), nameof(IsHiding))]
        public bool IsEnabled => !IsHidden && !IsHiding;

        public string LanguagePair { get; }

        public LearningInfoViewModel LearningInfoViewModel { get; }

        public string? Tooltip { get; }

        public WordViewModel Word { get; }

        public WordImageViewerViewModel WordImageViewerViewModel { get; }

        protected HashSet<Word> AcceptedAnswers { get; }

        protected IMessageHub MessageHub { get; }

        protected TranslationInfo TranslationInfo { get; }

        protected Func<Word, string, WordViewModel> WordViewModelFactory { get; }

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

        protected async Task HideControlWithTimeoutAsync(TimeSpan closeTimeout)
        {
            _logger.LogTrace("Hiding control in {0}...", closeTimeout);
            IsFocused = false;
            IsHiding = true;
            _assessmentBatchCardViewModel.NotifyChildIsClosing();
            await Task.Delay(closeTimeout).ConfigureAwait(true);
            IsHidden = true;
            IsHiding = false;
            _assessmentBatchCardViewModel.NotifyChildClosed();
            _logger.LogDebug("Control is hidden");
        }

        static bool HasUppercaseLettersExceptFirst(string text)
        {
            return text.Substring(1).Any(letter => char.IsLetter(letter) && char.IsUpper(letter));
        }

        static void PrepareVerb(string sourceLanguage, BaseWord word)
        {
            if ((sourceLanguage != LanguageConstants.EnLanguageTwoLetters) || (word.PartOfSpeech != PartOfSpeech.Verb))
            {
                return;
            }

            word.Text = "To " + (HasUppercaseLettersExceptFirst(word.Text) ? word.Text : word.Text.ToLowerInvariant());
        }

        async void HandleLearningInfoReceivedAsync(LearningInfo learningInfo)
        {
            _ = learningInfo ?? throw new ArgumentNullException(nameof(learningInfo));
            if (!learningInfo.Id.Equals(TranslationInfo.TranslationEntryKey))
            {
                return;
            }

            _logger.LogDebug("Received {0} from external source", learningInfo);

            await Task.Run(() => LearningInfoViewModel.UpdateLearningInfo(learningInfo), CancellationToken.None).ConfigureAwait(true);
        }

        async void HandleUiLanguageChangedAsync(CultureInfo cultureInfo)
        {
            _ = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            _logger.LogTrace("Changing UI language to {0}...", cultureInfo);

            await Task.Run(
                    () =>
                    {
                        _cultureManager.ChangeCulture(cultureInfo);
                        Word.ReRenderWord();
                    },
                    CancellationToken.None)
                .ConfigureAwait(true);
        }
    }
}
