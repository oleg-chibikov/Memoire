using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autofac;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
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
        private readonly ILearningInfoRepository _learningInfoRepository;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        private bool _hasOpenWindows;

        [NotNull]
        private IDisposable _interval;

        private TimeSpan _pausedTime;

        public AssessmentCardManager(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] ILifetimeScope lifetimeScope,
            [NotNull] IMessageHub messageHub,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ILearningInfoRepository learningInfoRepository)
            : base(lifetimeScope, localSettingsRepository, logger, synchronizationContext)
        {
            if (settingsRepository == null)
            {
                throw new ArgumentNullException(nameof(settingsRepository));
            }

            logger.Trace("Starting showing cards...");
            _interval = Disposable.Empty; //just to assign the field in the costructor
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _initTime = DateTime.Now;
            var localSettings = localSettingsRepository.Get();
            var settings = settingsRepository.Get();
            IsPaused = !localSettings.IsActive;
            CardShowFrequency = settings.CardShowFrequency;
            LastCardShowTime = localSettings.LastCardShowTime;
            _pausedTime = localSettings.PausedTime;
            if (!IsPaused)
            {
                //no await here
                CreateIntervalAsync();
            }
            else
            {
                LastPausedTime = DateTime.Now;
            }

            _subscriptionTokens.Add(messageHub.Subscribe<TimeSpan>(OnCardShowFrequencyChangedAsync));
            _subscriptionTokens.Add(messageHub.Subscribe<IntervalModificator>(OnIntervalModifiedAsync));
            logger.Debug("Started showing cards");
        }

        public bool IsPaused { get; private set; }

        public TimeSpan CardShowFrequency { get; private set; }

        public DateTime? LastCardShowTime { get; private set; }

        public DateTime NextCardShowTime => DateTime.Now + TimeLeftToShowCard;

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

        public async void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messageHub.UnSubscribe(subscriptionToken);
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);
            _interval.Dispose();
            _semaphore.Release();

            RecordPausedTime();

            Logger.Debug("Finished showing cards");
        }

        private async Task CreateIntervalAsync()
        {
            var delay = TimeLeftToShowCard;
            Logger.DebugFormat("Next card will be shown in: {0} (frequency is {1})", delay, CardShowFrequency);

            await _semaphore.WaitAsync().ConfigureAwait(false);
            _interval.Dispose();
            _interval = Observable.Timer(delay, CardShowFrequency)
                .Subscribe(
                    async x =>
                    {
                        Logger.Trace("Trying to show next card...");
                        Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                        var settings = LocalSettingsRepository.Get();
                        settings.PausedTime = _pausedTime = TimeSpan.Zero;

                        if (!settings.IsActive)
                        {
                            Logger.Debug("Skipped showing card due to inactivity");
                            LocalSettingsRepository.UpdateOrInsert(settings);
                            return;
                        }

                        settings.LastCardShowTime = LastCardShowTime = DateTime.Now;
                        LocalSettingsRepository.UpdateOrInsert(settings);
                        var mostSuitableLearningInfo = _learningInfoRepository.GetMostSuitable();
                        if (mostSuitableLearningInfo == null)
                        {
                            Logger.Debug("Skipped showing card due to absence of suitable cards");
                            return;
                        }

                        var translationEntry = _translationEntryRepository.GetById(mostSuitableLearningInfo.Id);

                        var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                            .ConfigureAwait(false);
                        var translationInfo = new TranslationInfo(translationEntry, translationDetails, mostSuitableLearningInfo);
                        Logger.TraceFormat("Trying to show {0}...", translationInfo);
                        ShowCard(translationInfo, null);
                    });
            _semaphore.Release();
        }

        private async void OnCardShowFrequencyChangedAsync(TimeSpan newCardShowFrequency)
        {
            await Task.Run(
                async () =>
                {
                    CardShowFrequency = newCardShowFrequency;
                    if (!IsPaused)
                    {
                        Logger.TraceFormat("Recreating interval for new frequency {0}...", newCardShowFrequency);
                        await CreateIntervalAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.DebugFormat("Skipped recreating interval for new frequency {0} as it is paused", newCardShowFrequency);
                    }
                },
                CancellationToken.None);
        }

        private async void OnIntervalModifiedAsync(IntervalModificator intervalModificator)
        {
            Logger.TraceFormat("Modifying interval: {0}...", intervalModificator.ToString());
            await Task.Run(
                    async () =>
                    {
                        switch (intervalModificator)
                        {
                            case IntervalModificator.Pause:
                                if (!IsPaused)
                                {
                                    await _semaphore.WaitAsync().ConfigureAwait(false);
                                    _interval.Dispose();
                                    _semaphore.Release();

                                    IsPaused = true;
                                    LastPausedTime = DateTime.Now;
                                }

                                break;
                            case IntervalModificator.Resume:
                                if (IsPaused)
                                {
                                    RecordPausedTime();
                                    await CreateIntervalAsync().ConfigureAwait(false);
                                }

                                break;
                        }
                    },
                    CancellationToken.None)
                .ConfigureAwait(false);
        }

        private void RecordPausedTime()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            var settings = LocalSettingsRepository.Get();
            settings.PausedTime = _pausedTime += DateTime.Now - LastPausedTime;
            Logger.DebugFormat("Paused time is {0}...", PausedTime);
            LocalSettingsRepository.UpdateOrInsert(settings);
        }

        protected override IWindow TryCreateWindow(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            if (_hasOpenWindows)
            {
                Logger.Trace("There is another window opened. Skipping creation...");
                return null;
            }

            Logger.TraceFormat("Creating window for {0}...", translationInfo);
            var learningInfo = _learningInfoRepository.GetById(translationInfo.TranslationEntryKey);
            learningInfo.ShowCount++; // single place to update show count - no need to synchronize
            learningInfo.LastCardShowTime = DateTime.Now;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(translationInfo.TranslationEntry);
            IWindow window;

            var nestedLifeTimeScope = LifetimeScope.BeginLifetimeScope();
            switch (learningInfo.RepeatType)
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
                    var assessmentViewModel = nestedLifeTimeScope.Resolve<AssessmentTextInputCardViewModel>(new TypedParameter(typeof(TranslationInfo), translationInfo));
                    window = nestedLifeTimeScope.Resolve<IAssessmentTextInputCardWindow>(
                        new TypedParameter(typeof(AssessmentTextInputCardViewModel), assessmentViewModel),
                        new TypedParameter(typeof(Window), ownerWindow));
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