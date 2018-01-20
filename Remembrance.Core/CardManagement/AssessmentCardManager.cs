using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.View.Card;
using Remembrance.ViewModel.Card;
using Scar.Common.WPF.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    [UsedImplicitly]
    internal sealed class AssessmentCardManager : BaseCardManager, IAssessmentCardManager, ICardShowTimeProvider, IDisposable
    {
        private readonly DateTime _initTime;

        [NotNull]
        private readonly object _intervalLocker = new object();

        private readonly Guid _intervalModifiedToken;

        [NotNull]
        private readonly IMessageHub _messenger;

        private readonly Guid _onCardShowFrequencyChangedToken;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        private bool _hasOpenWindows;

        [NotNull]
        private IDisposable _interval;

        private TimeSpan _pausedTime;

        public AssessmentCardManager(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessageHub messenger,
            [NotNull] IWordsProcessor wordsProcessor)
            : base(lifetimeScope, settingsRepository, logger)
        {
            logger.Info("Starting showing cards...");
            _interval = Disposable.Empty; //just to assign the field in the costructor
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _initTime = DateTime.Now;
            var settings = settingsRepository.Get();
            IsPaused = !settings.IsActive;
            CardShowFrequency = settings.CardShowFrequency;
            LastCardShowTime = settings.LastCardShowTime;
            _pausedTime = settings.PausedTime;
            if (!IsPaused)
            {
                CreateInterval();
            }
            else
            {
                LastPausedTime = DateTime.Now;
            }

            _onCardShowFrequencyChangedToken = messenger.Subscribe<TimeSpan>(OnCardShowFrequencyChanged);
            _intervalModifiedToken = messenger.Subscribe<IntervalModificator>(OnIntervalModified);
        }

        public bool IsPaused { get; private set; }

        public TimeSpan CardShowFrequency { get; private set; }

        public DateTime? LastCardShowTime { get; private set; }

        public DateTime LastPausedTime { get; private set; } = DateTime.MinValue;

        public TimeSpan PausedTime
        {
            get
            {
                if (!IsPaused)
                {
                    return _pausedTime;
                }

                return _pausedTime + (DateTime.Now - LastPausedTime);
            }
        }

        public TimeSpan TimeLeftToShowCard
        {
            get
            {
                var requiredInterval = CardShowFrequency + PausedTime;
                var alreadyPassedTime = DateTime.Now - (LastCardShowTime ?? _initTime);
                return alreadyPassedTime >= requiredInterval
                    ? TimeSpan.Zero
                    : requiredInterval - alreadyPassedTime;
            }
        }

        public void Dispose()
        {
            lock (_intervalLocker)
            {
                _interval.Dispose();
            }

            _messenger.UnSubscribe(_onCardShowFrequencyChangedToken);
            _messenger.UnSubscribe(_intervalModifiedToken);

            RecordPausedTime();

            Logger.Info("Finished showing cards");
        }

        private void CreateInterval()
        {
            var delay = TimeLeftToShowCard;
            Logger.Trace($"Next card will be shown in: {delay} (frequency is {CardShowFrequency})");
            lock (_intervalLocker)
            {
                _interval.Dispose();
                _interval = Observable.Timer(delay, CardShowFrequency)
                    .Subscribe(
                        async x =>
                        {
                            Logger.Trace("Trying to show next card...");
                            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                            var settings = SettingsRepository.Get();
                            settings.PausedTime = _pausedTime = TimeSpan.Zero;

                            if (!settings.IsActive)
                            {
                                Logger.Trace("Skipped showing card due to inactivity");
                                SettingsRepository.Save(settings);
                                return;
                            }

                            settings.LastCardShowTime = LastCardShowTime = DateTime.Now;
                            SettingsRepository.Save(settings);
                            var translationEntry = _translationEntryRepository.GetCurrent();
                            if (translationEntry == null)
                            {
                                Logger.Trace("Skipped showing card due to absence of suitable cards");
                                return;
                            }

                            var translationDetails = await _wordsProcessor.ReloadTranslationDetailsIfNeededAsync(
                                    translationEntry.Id,
                                    translationEntry.Key.Text,
                                    translationEntry.Key.SourceLanguage,
                                    translationEntry.Key.TargetLanguage,
                                    translationEntry.ManualTranslations,
                                    CancellationToken.None)
                                .ConfigureAwait(false);
                            var translationInfo = new TranslationInfo(translationEntry, translationDetails);
                            Logger.Trace($"Trying to show {translationInfo}...");
                            ShowCard(translationInfo, null);
                        });
            }
        }

        private void OnCardShowFrequencyChanged(TimeSpan newCardShowFrequency)
        {
            CardShowFrequency = newCardShowFrequency;
            if (!IsPaused)
            {
                Logger.Trace($"Recreating interval for new frequency {newCardShowFrequency}...");
                CreateInterval();
            }
            else
            {
                Logger.Trace($"Skipped recreating interval for new frequency {newCardShowFrequency} as it is paused");
            }
        }

        private void OnIntervalModified(IntervalModificator intervalModificator)
        {
            Logger.Trace($"Modifying interval: {intervalModificator.ToString()}...");
            switch (intervalModificator)
            {
                case IntervalModificator.Pause:
                    if (!IsPaused)
                    {
                        lock (_intervalLocker)
                        {
                            _interval.Dispose();
                        }

                        IsPaused = true;
                        LastPausedTime = DateTime.Now;
                    }

                    break;
                case IntervalModificator.Resume:
                    if (IsPaused)
                    {
                        RecordPausedTime();
                        CreateInterval();
                    }

                    break;
            }
        }

        private void RecordPausedTime()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            var settings = SettingsRepository.Get();
            settings.PausedTime = _pausedTime += DateTime.Now - LastPausedTime;
            Logger.Trace($"Paused time is {PausedTime}...");
            SettingsRepository.Save(settings);
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            if (_hasOpenWindows)
            {
                Logger.Trace("There is another window opened. Skipping creation...");
                return null;
            }

            Logger.Trace($"Creating window for {translationInfo}...");
            translationInfo.TranslationEntry.ShowCount++; // single place to update show count - no need to synchronize
            translationInfo.TranslationEntry.LastCardShowTime = DateTime.Now;
            _translationEntryRepository.Save(translationInfo.TranslationEntry);
            _messenger.Publish(translationInfo);
            IWindow window;

            var nestedLifeTimeScope = LifetimeScope.BeginLifetimeScope();
            switch (translationInfo.TranslationEntry.RepeatType)
            {
                case RepeatType.Elementary:
                case RepeatType.Beginner:
                case RepeatType.Novice:
                {
                    var assessmentViewModel = nestedLifeTimeScope.Resolve<AssessmentViewOnlyCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = nestedLifeTimeScope.Resolve<IAssessmentViewOnlyCardWindow>(new TypedParameter(typeof(AssessmentViewOnlyCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                case RepeatType.PreIntermediate:
                case RepeatType.Intermediate:
                case RepeatType.UpperIntermediate:
                {
                    //TODO: Dropdown
                    var assessmentViewModel = nestedLifeTimeScope.Resolve<AssessmentViewOnlyCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = nestedLifeTimeScope.Resolve<IAssessmentViewOnlyCardWindow>(new TypedParameter(typeof(AssessmentViewOnlyCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                case RepeatType.Advanced:
                case RepeatType.Proficiency:
                //TODO: Reverse and random trans for high levels
                case RepeatType.Expert:
                {
                    var assessmentViewModel = nestedLifeTimeScope.Resolve<AssessmentTextInputCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = nestedLifeTimeScope.Resolve<IAssessmentTextInputCardWindow>(
                        new TypedParameter(typeof(AssessmentTextInputCardViewModel), assessmentViewModel),
                        new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            window.Closed += Window_Closed;
            window.AssociateDisposable(nestedLifeTimeScope);
            _hasOpenWindows = true;
            return window;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _hasOpenWindows = false;
            ((Window)sender).Closed -= Window_Closed;
        }
    }
}