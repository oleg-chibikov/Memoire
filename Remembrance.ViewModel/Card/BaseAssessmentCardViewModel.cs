// TODO: Feature: if the word level is low, replace textbox with dropdown

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using PropertyChanged;
using Remembrance.Contracts;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate.Data.WordsTranslator;
using Remembrance.Resources;
using Remembrance.ViewModel.Translation;
using Scar.Common;
using Scar.Common.Exceptions;
using Scar.Common.WPF.Commands;
using Scar.Common.WPF.Localization;
using Scar.Common.WPF.ViewModel;

namespace Remembrance.ViewModel.Card
{
    //TODO: Test delete file then save or smth
    [AddINotifyPropertyChangedInterface]
    public abstract class BaseAssessmentCardViewModel : IRequestCloseViewModel, IDisposable
    {
        [NotNull]
        private static readonly Random Random = new Random();

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly SynchronizationContext _synchronizationContext;

        [NotNull]
        protected readonly HashSet<WordViewModel> AcceptedAnswers;

        protected readonly TimeSpan FailureCloseTimeout;

        [NotNull]
        protected readonly ILog Logger;

        [NotNull]
        protected readonly IMessageHub Messenger;

        protected readonly TimeSpan SuccessCloseTimeout;

        [NotNull]
        protected readonly TranslationInfo TranslationInfo;

        [NotNull]
        protected readonly IViewModelAdapter ViewModelAdapter;

        protected BaseAssessmentCardViewModel(
            [NotNull] TranslationInfo translationInfo,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] IViewModelAdapter viewModelAdapter,
            [NotNull] IMessageHub messenger,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] SynchronizationContext synchronizationContext)
        {
            if (settingsRepository == null)
            {
                throw new ArgumentNullException(nameof(settingsRepository));
            }

            ViewModelAdapter = viewModelAdapter ?? throw new ArgumentNullException(nameof(viewModelAdapter));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            TranslationInfo = translationInfo ?? throw new ArgumentNullException(nameof(translationInfo));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));

            logger.Trace("Pausing showing cards...");
            messenger.Publish(IntervalModificator.Pause);

            LanguagePair = $"{TranslationInfo.TranslationEntryKey.SourceLanguage} -> {TranslationInfo.TranslationEntryKey.TargetLanguage}";

            var translationDetailsViewModel = viewModelAdapter.Adapt<TranslationDetailsViewModel>(translationInfo);
            var translationResult = translationDetailsViewModel.TranslationResult;

            logger.Trace("Initializing card...");
            var settings = settingsRepository.Get();
            SuccessCloseTimeout = settings.AssessmentSuccessCloseTimeout;
            FailureCloseTimeout = settings.AssessmentFailureCloseTimeout;
            var hasPriorityItems = FilterPriorityPartOfSpeechTranslations(translationResult);
            var partOfSpeechGroup = SelectSinglePartOfSpeechGroup(settings, translationResult);
            var acceptedWordGroups = GetAcceptedWordGroups(partOfSpeechGroup);
            var needRandom = hasPriorityItems || settings.RandomTranslation;
            var assessmentInfo = IsReverse(settings)
                ? GetReverseAssessmentInfo(needRandom, acceptedWordGroups)
                : GetStraightAssessmentInfo(acceptedWordGroups);
            TranslationDetailsCardViewModel = lifetimeScope.Resolve<TranslationDetailsCardViewModel>(new TypedParameter(typeof(TranslationInfo), TranslationInfo));
            AcceptedAnswers = assessmentInfo.AcceptedAnswers;
            assessmentInfo.Word.CanLearnWord = false;
            Word = assessmentInfo.Word;
            CorrectAnswer = assessmentInfo.CorrectAnswer;
            WindowClosedCommand = new CorrelationCommand(WindowClosed);

            _subscriptionTokens.Add(messenger.Subscribe<CultureInfo>(OnUiLanguageChangedAsync));
        }

        [NotNull]
        public TranslationDetailsCardViewModel TranslationDetailsCardViewModel { get; }

        [NotNull]
        public ICommand WindowClosedCommand { get; }

        [NotNull]
        public WordViewModel Word { get; }

        [NotNull]
        public string LanguagePair { get; }

        [NotNull]
        public WordViewModel CorrectAnswer { get; protected set; }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                Messenger.UnSubscribe(subscriptionToken);
            }
        }

        public event EventHandler RequestClose;

        /// <summary>
        /// If there are any priority translations - leave only them, otherwise leave all.
        /// </summary>
        private void FilterAcceptedWordsGroupsByPriority([NotNull] ref KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>[] acceptedWordGroups)
        {
            Logger.Trace("Filtering accepted words groups by priority...");
            var tmp = new List<KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>>();

            foreach (var acceptedWordGroup in acceptedWordGroups)
            {
                var lst = acceptedWordGroup.Value.ToList();
                lst.RemoveAll(x => !x.IsPriority);
                if (lst.Any())
                {
                    tmp.Add(new KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]>(acceptedWordGroup.Key, lst.ToArray()));
                }
            }

            if (tmp.Any())
            {
                Logger.DebugFormat("There are {0} groups that contain priority translations. Filtering was applied", tmp.Count);
                acceptedWordGroups = tmp.ToArray();
            }

            Logger.Debug("There are no groups that contain priority translations. Filtering was not applied");
        }

        /// <summary>
        /// If there are any priority translations - leave only their part of speech groups, otherwise leave all.
        /// </summary>
        /// <returns>Try - has priority items</returns>
        private bool FilterPriorityPartOfSpeechTranslations([NotNull] TranslationResultViewModel translationResult)
        {
            Logger.Trace("Filtering translations by priority...");
            var priorityPartOfSpeechTranslations = translationResult.PartOfSpeechTranslations.ToList();
            priorityPartOfSpeechTranslations.RemoveAll(
                partOfSpeechTranslation =>
                    !partOfSpeechTranslation.TranslationVariants.Any(translationVariant => translationVariant.IsPriority || translationVariant.Synonyms?.Any(synonym => synonym.IsPriority) == true));
            var hasPriorityItems = priorityPartOfSpeechTranslations.Any();
            if (hasPriorityItems)
            {
                Logger.DebugFormat("There are {0} priority translations. Filtering was applied", priorityPartOfSpeechTranslations.Count);
                translationResult.PartOfSpeechTranslations = priorityPartOfSpeechTranslations.ToArray();
            }
            else
            {
                Logger.Debug("There are no priority translations. Filtering was not applied");
            }

            return hasPriorityItems;
        }

        /// <summary>
        /// Get all possible original word variants of this part of speech
        /// </summary>
        [NotNull]
        private KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>[] GetAcceptedWordGroups([NotNull] IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel> partOfSpeechGroup)
        {
            Logger.TraceFormat("Getting accepted words groups for {0}...", partOfSpeechGroup.Key);
            var acceptedWordGroups = partOfSpeechGroup.SelectMany(
                    partOfSpeechTranslation => partOfSpeechTranslation.TranslationVariants.Select(traslationVariant => GetPossibleTranslations(traslationVariant, partOfSpeechTranslation)))
                .ToArray();
            if (!acceptedWordGroups.Any())
            {
                throw new LocalizableException(Errors.NoTranslations, "No translations found");
            }

            Logger.DebugFormat("There are {0} accepted words groups", acceptedWordGroups.Length);
            FilterAcceptedWordsGroupsByPriority(ref acceptedWordGroups);

            return acceptedWordGroups.Select(x => new KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>(x.Key, ViewModelAdapter.Adapt<WordViewModel[]>(x.Value))).ToArray();
        }

        /// <summary>
        /// Get all possible translations (including synonyms)
        /// </summary>
        private KeyValuePair<PartOfSpeechTranslationViewModel, PriorityWordViewModel[]> GetPossibleTranslations(
            [NotNull] TranslationVariantViewModel traslationVariant,
            [NotNull] PartOfSpeechTranslationViewModel partOfSpeechTranslation)
        {
            Logger.TraceFormat("Getting accepted words groups for {0}...", traslationVariant);
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
            Logger.Trace("Getting random part of speech group...");
            var randomPartOfSpeechGroupIndex = Random.Next(partOfSpeechGroups.Length);
            var result = partOfSpeechGroups.ElementAt(randomPartOfSpeechGroupIndex);
            return result;
        }

        /// <summary>
        /// Decide whether the Word would be chosen randomly or not
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfo(bool needRandom, [NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>[] acceptedWordGroups)
        {
            Logger.Trace("Getting reverse assessment info...");
            return needRandom
                ? GetReverseAssessmentInfoFromRandomTranslation(acceptedWordGroups)
                : GetReverseAssessmentInfoFromFirstTranslation(acceptedWordGroups);
        }

        /// <summary>
        /// The first variant for part of speech will be selected and the first translation inside it
        /// </summary>
        [NotNull]
        private AssessmentInfo GetReverseAssessmentInfoFromFirstTranslation([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>[] acceptedWordGroups)
        {
            Logger.Trace("Getting info from first translation...");
            var acceptedWordGroup = acceptedWordGroups.First();
            var translation = acceptedWordGroup.Value.First();
            var correct = acceptedWordGroup.Key;
            return new AssessmentInfo(
                new HashSet<WordViewModel>
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
        private AssessmentInfo GetReverseAssessmentInfoFromRandomTranslation([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>[] acceptedWordGroups)
        {
            Logger.Trace("Getting info from random translation...");
            var randomAcceptedWordGroupIndex = Random.Next(acceptedWordGroups.Length);
            var randomAcceptedWordGroup = acceptedWordGroups.ElementAt(randomAcceptedWordGroupIndex);
            var randomTranslationIndex = Random.Next(randomAcceptedWordGroup.Value.Length);
            var randomTranslation = randomAcceptedWordGroup.Value[randomTranslationIndex];
            var correct = randomAcceptedWordGroup.Key;
            return new AssessmentInfo(
                new HashSet<WordViewModel>
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
        private AssessmentInfo GetStraightAssessmentInfo([NotNull] KeyValuePair<PartOfSpeechTranslationViewModel, WordViewModel[]>[] acceptedWordGroups)
        {
            Logger.Trace("Getting straight assessment info...");
            var accept = new HashSet<WordViewModel>(acceptedWordGroups.SelectMany(x => x.Value));
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
            {
                isReverse = Random.Next(2) == 1;
            }

            return isReverse;
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
                CancellationToken.None);
        }

        /// <summary>
        /// Choose the single part of speech group
        /// </summary>
        [NotNull]
        private IGrouping<PartOfSpeech, PartOfSpeechTranslationViewModel> SelectSinglePartOfSpeechGroup([NotNull] Contracts.DAL.Model.Settings settings, [NotNull] TranslationResultViewModel translationResult)
        {
            Logger.Trace("Selecting single part of speech group...");
            var partOfSpeechGroups = translationResult.PartOfSpeechTranslations.GroupBy(x => x.PartOfSpeech).ToArray();
            var partOfSpeechGroup = settings.RandomTranslation
                ? GetRandomPartOfSpeechGroup(partOfSpeechGroups)
                : partOfSpeechGroups.First();
            if (partOfSpeechGroup == null)
            {
                throw new LocalizableException(Errors.NoTranslations, "No translations found");
            }

            return partOfSpeechGroup;
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

        private void WindowClosed()
        {
            Logger.Trace("Resuming showing cards...");
            Messenger.Publish(IntervalModificator.Resume);
        }

        private sealed class AssessmentInfo
        {
            public AssessmentInfo([NotNull] HashSet<WordViewModel> acceptedAnswers, [NotNull] WordViewModel word, [NotNull] WordViewModel correctAnswer)
            {
                AcceptedAnswers = acceptedAnswers;
                Word = word;
                CorrectAnswer = correctAnswer;
            }

            [NotNull]
            public HashSet<WordViewModel> AcceptedAnswers { get; }

            [NotNull]
            public WordViewModel Word { get; }

            [NotNull]
            public WordViewModel CorrectAnswer { get; }
        }
    }
}