using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Contracts.TypeAdapter;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.ViewModel;

//TODO: regions
//TODO: Feature: if the word level is low, replace textbox with dropdown

namespace Remembrance.ViewModel.Card
{
    [UsedImplicitly]
    [AddINotifyPropertyChangedInterface]
    public sealed class AssessmentCardViewModel : IRequestCloseViewModel
    {
        [NotNull]
        private static readonly Random Random = new Random();

        //TODO: config timeout
        private static readonly TimeSpan SuccessCloseTimeout = TimeSpan.FromSeconds(2);

        //TODO: config timeout
        private static readonly TimeSpan ErrorCloseTimeout = TimeSpan.FromSeconds(5);

        [NotNull]
        private readonly HashSet<string> _acceptedAnswers;

        [NotNull]
        private readonly SynchronizationContext _syncContext = SynchronizationContext.Current;

        public AssessmentCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessenger messenger,
            [NotNull] ILog logger)
        {
            if (settingsRepository == null)
                throw new ArgumentNullException(nameof(settingsRepository));
            if (viewModelAdapter == null)
                throw new ArgumentNullException(nameof(viewModelAdapter));

            _translationInfo = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ProvideAnswerCommand = new CorrelationCommand<string>(ProvideAnswer);

            LanguagePair = $"{_translationInfo.Key.TargetLanguage} - {_translationInfo.Key.SourceLanguage}";

            var translationDetailsViewModel = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            var translationResult = translationDetailsViewModel.TranslationResult;

            logger.Trace("Initializing card...");
            var settings = settingsRepository.Get();
            var hasPriorityItems = FilterPriorityPartOfSpeechTranslations(translationResult);
            var partOfSpeechGroup = SelectSinglePartOfSpeechGroup(settings, translationResult);
            var acceptedWordGroups = GetAcceptedWordGroups(partOfSpeechGroup);
            var needRandom = hasPriorityItems || settings.RandomTranslation;
            var assessmentInfo = IsReverse(settings)
                ? GetReverseAssessmentInfo(needRandom, acceptedWordGroups)
                : GetStraightAssessmentInfo(acceptedWordGroups);
            _acceptedAnswers = assessmentInfo.AcceptedAnswers;
            assessmentInfo.Word.CanLearnWord = false;
            Word = assessmentInfo.Word;
            CorrectAnswer = assessmentInfo.CorrectAnswer;

            messenger.Register<string>(this, MessengerTokens.UiLanguageToken, OnUiLanguageChanged);
            logger.Trace("Card is initialized");
        }

        public WordViewModel Word { get; }

        public string LanguagePair { get; }

        #region Commands

        public ICommand ProvideAnswerCommand { get; }

        #endregion

        public event EventHandler RequestClose;

        /// <summary>
        /// If there are any priority translations - leave only them, otherwise leave all.
        /// </summary>
        private void FilterAcceptedWordsGroupsByPriority([NotNull] ref KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            _logger.Trace("Filtering accepted words groups by priority...");
            var tmp = new List<KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>>();

            foreach (var acceptedWordGroup in acceptedWordGroups)
            {
                var lst = acceptedWordGroup.Value.ToList();
                lst.RemoveAll(x => !x.IsPriority);
                if (lst.Any())
                    tmp.Add(new KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>(acceptedWordGroup.Key, lst.ToArray()));
            }

            if (tmp.Any())
            {
                _logger.Trace($"There are {tmp.Count} groups that contain priority translations. Filtering was applied");
                acceptedWordGroups = tmp.ToArray();
            }
            _logger.Trace("There are no groups that contain priority translations. Filtering was not applied");
        }

        /// <summary>
        /// If there are any priority translations - leave only their part of speech groups, otherwise leave all.
        /// </summary>
        /// <returns>Try - has priority items</returns>
        private bool FilterPriorityPartOfSpeechTranslations([NotNull] TranslationResultViewModel translationResult)
        {
            _logger.Trace("Filtering translations by priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(
                partOfSpeechTranslation => !partOfSpeechTranslation.TranslationVariants.Any(
                    translationVariant => translationVariant.IsPriority || translationVariant.Synonyms?.Any(synonym => synonym.IsPriority) == true));
            var hasPriorityItems = priorityPartOfSpeechTranslations.Any();
            if (hasPriorityItems)
            {
                _logger.Trace($"There are {priorityPartOfSpeechTranslations.Count} priority translations. Filtering was applied");
                translationResult.PartOfSpeechTranslations = priorityPartOfSpeechTranslations.ToArray();
            }
            else
            {
                _logger.Trace("There are no priority translations. Filtering was not applied");
            }
            return hasPriorityItems;
        }

        /// <summary>
        /// Get all possible original word variants of this part of speech
        /// </summary>
        [NotNull]
        private KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] GetAcceptedWordGroups([NotNull] IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel> partOfSpeechGroup)
        {
            _logger.Trace($"Getting accepted words groups for {partOfSpeechGroup.Key}...");
            var acceptedWordGroups = partOfSpeechGroup.SelectMany(
                    partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants.Select(traslationVariant => GetPossibleTranslations(traslationVariant, partOfSpeechTranslation)))
                .ToArray();
            if (!acceptedWordGroups.Any())
                throw new InvalidOperationException(Errors.NoTranslations);

            _logger.Trace($"There are {acceptedWordGroups.Length} accepted words groups");
            FilterAcceptedWordsGroupsByPriority(ref acceptedWordGroups);
            return acceptedWordGroups;
        }

        /// <summary>
        /// Get all possible translations (including synonyms)
        /// </summary>
        private KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]> GetPossibleTranslations(
            [NotNull] TranslationVariantViewModel traslationVariant,
            [NotNull] PartOfSpeechTranslationViewModel partOfSpeechTranslation)
        {
            _logger.Trace($"Getting accepted words groups for {traslationVariant}...");
            var translations = traslationVariant.Synonyms != null
                ? new[]
                    {
                        traslationVariant
                    }.Concat(traslationVariant.Synonyms.Select(synonym => synonym))
                    .ToArray()
                : new[]
                {
                    traslationVariant
                };
            return new KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>(partOfSpeechTranslation, translations);
        }

        /// <summary>
        /// Get the most suitable part of speech group according to POS priorities
        /// </summary>
        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel> GetRandomPartOfSpeechGroup([NotNull] IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel>[] partOfSpeechGroups)
        {
            _logger.Trace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Length);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        /// <summary>
        /// Decide whether the Word would be chosen randomly or not
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfo(bool needRandom, [NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            _logger.Trace("Getting reverse assessment info...");
            return needRandom
                ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups)
                : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        /// <summary>
        /// The first variant for part of speech will be selected and the first translation inside it
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            _logger.Trace("Getting info from first translation...");
            var acceptedWordGroup = acceptedWordGroups.First();
            var translation = acceptedWordGroup.Value.First();
            var correct = acceptedWordGroup.Key.Text;
            return new AssessmentInfo(
                new HashSet<string>
                {
                    correct
                },
                translation,
                correct);
        }

        /// <summary>
        /// Random variant for part of speech will be selected and random translation inside it
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            _logger.Trace("Getting info from random translation...");
            var randomAcceptedWordGroupIndex = Random.Next(acceptedWordGroups.Length);
            var randomAcceptedWordGroup = acceptedWordGroups.ElementAt(randomAcceptedWordGroupIndex);
            var randomTranslationIndex = Random.Next(randomAcceptedWordGroup.Value.Length);
            var randomTranslation = randomAcceptedWordGroup.Value[randomTranslationIndex];
            var correct = randomAcceptedWordGroup.Key.Text;
            return new AssessmentInfo(
                new HashSet<string>
                {
                    correct
                },
                randomTranslation,
                correct);
        }

        /// <summary>
        /// Settings the first variant in the group as the Word and all possible variants as the Acceptable answers
        /// </summary>
        [NotNull]
        private AssessmentInfo GetStraightAssessmentInfo([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            _logger.Trace("Getting straight assessment info...");
            var accept = new HashSet<string>(acceptedWordGroups.SelectMany(x => x.Value.Select(v => v.Text)));
            var word = acceptedWordGroups.First().Key;
            var correct = accept.First();
            return new AssessmentInfo(accept, word, correct);
        }

        /// <summary>
        /// Decide whether reverse translation is needed
        /// </summary>
        private static bool IsReverse([NotNull] Contracts.DAL.Model.Settings settings)
        {
            var isReverse = false;
            if (settings.ReverseTranslation)
                isReverse = Random.Next(2) == 1;
            return isReverse;
        }

        private void OnUiLanguageChanged([NotNull] string uiLanguage)
        {
            _logger.Trace($"Changing UI language to {uiLanguage}...");
            if (uiLanguage == null)
                throw new ArgumentNullException(nameof(uiLanguage));

            CultureUtilities.ChangeCulture(uiLanguage);
            Word.ReRender();
        }

        private void ProvideAnswer([CanBeNull] string answer)
        {
            _logger.Info($"Providing answer {answer}...");
            string mostSuitable = null;
            var currentMinDistance = int.MaxValue;
            if (!string.IsNullOrWhiteSpace(answer))
                foreach (var acceptedAnswer in _acceptedAnswers)
                {
                    //20% of the word could be errors
                    var maxDistance = acceptedAnswer.Length / 5;
                    var distance = answer.LevenshteinDistance(acceptedAnswer);
                    if (distance < 0 || distance > maxDistance)
                        continue;

                    if (distance < currentMinDistance)
                    {
                        currentMinDistance = distance;
                        mostSuitable = acceptedAnswer;
                    }
                    Accepted = true;
                }

            TimeSpan closeTimeout;

            if (Accepted == true)
            {
                _logger.Info($"Answer is correct. Most suitable accepted word was {mostSuitable} with distance {currentMinDistance}. Increasing repeat type for {_translationInfo}...");
                _translationInfo.TranslationEntry.IncreaseRepeatType();
                //The inputed answer can differ from the first one
                // ReSharper disable once AssignNullToNotNullAttribute - mostSuitable should be always set
                CorrectAnswer = mostSuitable;
                closeTimeout = SuccessCloseTimeout;
            }
            else
            {
                _logger.Info($"Answer is not correct. Decreasing repeat type for {_translationInfo}...");
                Accepted = false;
                _translationInfo.TranslationEntry.DecreaseRepeatType();
                closeTimeout = ErrorCloseTimeout;
            }
            _translationEntryRepository.Save(_translationInfo.TranslationEntry);
            _messenger.Send(_translationInfo, MessengerTokens.TranslationInfoToken);
            _logger.Trace($"Closing window in {closeTimeout}...");
            ActionExtensions.DoAfterAsync(
                () =>
                {
                    _syncContext.Post(x => RequestClose?.Invoke(null, null), null);
                    _logger.Trace("Window is closed");
                },
                closeTimeout);
        }

        /// <summary>
        /// Choose the single part of speech group
        /// </summary>
        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel> SelectSinglePartOfSpeechGroup([NotNull] Contracts.DAL.Model.Settings settings, [NotNull] TranslationResultViewModel translationResult)
        {
            _logger.Trace("Selecting single part of speech group...");
            var partOfSpeechGroups = translationResult.PartOfSpeechTranslations.GroupBy(x => x.PartOfSpeech).ToArray();
            var partOfSpeechGroup = settings.RandomTranslation
                ? GetRandomPartOfSpeechGroup(partOfSpeechGroups)
                : partOfSpeechGroups.First();
            if (partOfSpeechGroup == null)
                throw new InvalidOperationException(Errors.NoTranslations);

            return partOfSpeechGroup;
        }

        private sealed class AssessmentInfo
        {
            public AssessmentInfo(HashSet<string> acceptedAnswers, WordViewModel word, string correctAnswer)
            {
                AcceptedAnswers = acceptedAnswers;
                Word = word;
                CorrectAnswer = correctAnswer;
            }

            public HashSet<string> AcceptedAnswers { get; }
            public WordViewModel Word { get; }

            public string CorrectAnswer { get; }
        }

        #region Dependency Properties

        public bool? Accepted { get; private set; }

        public string CorrectAnswer { get; private set; }

        #endregion

        #region Dependencies

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly TranslationInfo _translationInfo;

        #endregion
    }
}