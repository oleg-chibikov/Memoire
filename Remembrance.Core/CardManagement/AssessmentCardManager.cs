using System;
using System.Diagnostics;
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
    internal sealed class AssessmentCardManager : BaseCardManager, IAssessmentCardManager, IDisposable
    {
        private readonly DateTime _initTime;
        private readonly Guid _intervalModifiedToken;

        [NotNull]
        private readonly IMessageHub _messenger;

        private readonly Guid _onCardShowFrequencyChangedToken;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        [NotNull]
        private readonly object intervalLocker = new object();

        private TimeSpan _cardShowFrequency;

        private bool _hasOpenWindows;

        [NotNull]
        private IDisposable _interval;

        private bool _isPaused;

        [CanBeNull]
        private DateTime? _lastCardShowTime;

        [CanBeNull]
        private DateTime? _lastPausedTime;

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

            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _initTime = DateTime.Now;
            var settings = settingsRepository.Get();
            _isPaused = !settings.IsActive;
            _cardShowFrequency = settings.CardShowFrequency;
            _lastCardShowTime = settings.LastCardShowTime;
            _pausedTime = settings.PausedTime;
            if (!_isPaused)
                CreateInterval();
            _onCardShowFrequencyChangedToken = messenger.Subscribe<TimeSpan>(OnCardShowFrequencyChanged);
            _intervalModifiedToken = messenger.Subscribe<IntervalModificator>(OnIntervalModified);
        }

        private TimeSpan TimeLeftToShowCard
        {
            get
            {
                var requiredInterval = _cardShowFrequency + _pausedTime;
                var alreadyPassedTime = DateTime.Now - (_lastCardShowTime ?? _initTime);
                return alreadyPassedTime >= requiredInterval
                    ? TimeSpan.Zero
                    : requiredInterval - alreadyPassedTime;
            }
        }

        public void Dispose()
        {
            lock (intervalLocker)
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
            Logger.Trace($"Next card will be shown in: {delay} (frequency is {_cardShowFrequency})");
            lock (intervalLocker)
            {
                _interval = Observable.Timer(delay, _cardShowFrequency)
                    .Subscribe(
                        async x =>
                        {
                            Logger.Trace("Trying to show next card...");
                            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                            var settings = SettingsRepository.Get();
                            settings.PausedTime = _pausedTime = TimeSpan.Zero;
                            _lastPausedTime = null;

                            if (!settings.IsActive)
                            {
                                Logger.Trace("Skipped showing card due to inactivity");
                                SettingsRepository.Save(settings);
                                return;
                            }

                            settings.LastCardShowTime = _lastCardShowTime = DateTime.Now;
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
            _cardShowFrequency = newCardShowFrequency;
            if (!_isPaused)
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
                    if (!_isPaused)
                    {
                        lock (intervalLocker)
                        {
                            _interval.Dispose();
                        }
                        _isPaused = true;
                        _lastPausedTime = DateTime.Now;
                    }
                    break;
                case IntervalModificator.Resume:
                    if (_isPaused)
                    {
                        RecordPausedTime();
                        CreateInterval();
                    }
                    break;
            }
        }

        private void RecordPausedTime()
        {
            _isPaused = false;
            if (_lastPausedTime != null)
            {
                var settings = SettingsRepository.Get();
                settings.PausedTime = _pausedTime += DateTime.Now - _lastPausedTime.Value;
                Logger.Trace($"Paused time is {_pausedTime}...");
                SettingsRepository.Save(settings);
                _lastPausedTime = null;
            }
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
            switch (translationInfo.TranslationEntry.RepeatType)
            {
                case RepeatType.Elementary:
                case RepeatType.Beginner:
                case RepeatType.Novice:
                {
                    var assessmentViewModel = LifetimeScope.Resolve<AssessmentViewOnlyCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = LifetimeScope.Resolve<IAssessmentViewOnlyCardWindow>(new TypedParameter(typeof(AssessmentViewOnlyCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                case RepeatType.PreIntermediate:
                case RepeatType.Intermediate:
                case RepeatType.UpperIntermediate:
                {
                    //TODO: Dropdown
                    var assessmentViewModel = LifetimeScope.Resolve<AssessmentTextInputCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = LifetimeScope.Resolve<IAssessmentTextInputCardWindow>(new TypedParameter(typeof(AssessmentTextInputCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                case RepeatType.Advanced:
                case RepeatType.Proficiency:
                //TODO: Reverse and random trans for high levels
                case RepeatType.Expert:
                {
                    var assessmentViewModel = LifetimeScope.Resolve<AssessmentTextInputCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = LifetimeScope.Resolve<IAssessmentTextInputCardWindow>(new TypedParameter(typeof(AssessmentTextInputCardViewModel), assessmentViewModel), new TypedParameter(typeof(Window), ownerWindow));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            window.Closed += Window_Closed;
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