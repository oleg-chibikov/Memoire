using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.CardManagement;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Mémoire.Contracts.View.Card;
using Microsoft.Extensions.Logging;
using Scar.Common.View.Contracts;
using Scar.Common.View.WindowCreation;

namespace Mémoire.Core.CardManagement
{
    public sealed class AssessmentCardManager : IAssessmentCardManager, ICardShowTimeProvider, IDisposable
    {
        readonly DateTimeOffset _initTime;
        readonly ILearningInfoRepository _learningInfoRepository;
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly ISharedSettingsRepository _sharedSettingsRepository;
        readonly object _lockObject = new ();
        readonly IMessageHub _messageHub;
        readonly IPauseManager _pauseManager;
        readonly IScopedWindowProvider _scopedWindowProvider;
        readonly IList<Guid> _subscriptionTokens = new List<Guid>();
        readonly ITranslationEntryProcessor _translationEntryProcessor;
        readonly ITranslationEntryRepository _translationEntryRepository;
        readonly ILogger _logger;
        readonly SynchronizationContext _synchronizationContext;
        readonly IWindowPositionAdjustmentManager _windowPositionAdjustmentManager;
        bool _hasOpenWindows;
        IDisposable _interval = Disposable.Empty;

        public AssessmentCardManager(
            ITranslationEntryRepository translationEntryRepository,
            IMessageHub messageHub,
            ITranslationEntryProcessor translationEntryProcessor,
            ILocalSettingsRepository localSettingsRepository,
            SynchronizationContext synchronizationContext,
            ILearningInfoRepository learningInfoRepository,
            IScopedWindowProvider scopedWindowProvider,
            IPauseManager pauseManager,
            ISharedSettingsRepository sharedSettingsRepository,
            ILogger<AssessmentCardManager> logger,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _synchronizationContext = synchronizationContext ?? throw new ArgumentNullException(nameof(synchronizationContext));
            _windowPositionAdjustmentManager = windowPositionAdjustmentManager ?? throw new ArgumentNullException(nameof(windowPositionAdjustmentManager));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _initTime = DateTimeOffset.Now;

            CardShowFrequency = sharedSettingsRepository.CardShowFrequency;
            LastCardShowTime = localSettingsRepository.LastCardShowTime;

            if (!_pauseManager.IsPaused)
            {
                CreateInterval();
            }

            _subscriptionTokens.Add(messageHub.Subscribe<TimeSpan>(HandleCardShowFrequencyChanged));
            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReasonAndState>(HandlePauseReasonChanged));
            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public TimeSpan CardShowFrequency { get; private set; }

        public DateTimeOffset? LastCardShowTime { get; private set; }

        public DateTimeOffset NextCardShowTime => DateTimeOffset.Now + TimeLeftToShowCard;

        public TimeSpan TimeLeftToShowCard
        {
            get
            {
                var requiredInterval = CardShowFrequency + _pauseManager.GetPauseInfo(PauseReasons.CardIsVisible).PauseTime;
                var alreadyPassedTime = DateTimeOffset.Now - (LastCardShowTime ?? _initTime);
                return alreadyPassedTime >= requiredInterval ? TimeSpan.Zero : requiredInterval - alreadyPassedTime;
            }
        }

        public void Dispose()
        {
            foreach (var subscriptionToken in _subscriptionTokens)
            {
                _messageHub.Unsubscribe(subscriptionToken);
            }

            _subscriptionTokens.Clear();

            lock (_lockObject)
            {
                _interval.Dispose();
            }

            _logger.LogDebug("Finished showing cards");
        }

        void CreateInterval()
        {
            var delay = TimeLeftToShowCard;
            _logger.LogDebug("Next card will be shown in: {Delay} (frequency is {CardShowFrequency})", delay, CardShowFrequency);

            lock (_lockObject)
            {
                _interval.Dispose();
                _interval = ProvideInterval(delay);
            }
        }

        void HandleCardShowFrequencyChanged(TimeSpan newCardShowFrequency)
        {
            CardShowFrequency = newCardShowFrequency;
            if (!_pauseManager.IsPaused)
            {
                _logger.LogTrace("Recreating interval for new frequency {CardShowFrequency}...", newCardShowFrequency);
                lock (_lockObject)
                {
                    CreateInterval();
                }
            }
            else
            {
                _logger.LogDebug("Skipped recreating interval for new frequency {CardShowFrequency} as it is paused", newCardShowFrequency);
            }
        }

        async void HandleIntervalHitAsync(long x)
        {
            // TODO: this should not happen together with app close, otherwise the app may crash.
            _pauseManager.ResetPauseTimes();
            _pauseManager.PauseActivity(PauseReasons.CardIsLoading);
            try
            {
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                _logger.LogTrace("Trying to show next card...");

                if (!_localSettingsRepository.IsActive)
                {
                    _logger.LogDebug("Skipped showing card due to inactivity");
                    return;
                }

                if (_hasOpenWindows)
                {
                    _logger.LogTrace("There is another window opened. Skipping creation...");
                    return;
                }

                _localSettingsRepository.LastCardShowTime = LastCardShowTime = DateTimeOffset.Now;
                var mostSuitableLearningInfos = _learningInfoRepository.GetMostSuitable(_sharedSettingsRepository.CardsToShowAtOnce).ToArray();
                if (mostSuitableLearningInfos.Length == 0)
                {
                    _logger.LogDebug("Skipped showing card due to absence of suitable cards");
                    return;
                }

                IAsyncEnumerable<TranslationInfo> translationInfos;
                try
                {
                    translationInfos = GetTranslationInfosForAllWordsAsync(mostSuitableLearningInfos);
                }
                catch (InvalidOperationException ex)
                {
                    // If the word cannot be found in DB // TODO: additional handling? get it from somewhere
                    _messageHub.Publish(ex);
                    return;
                }

                await CreateAndShowWindowAsync(await translationInfos.ToArrayAsync().ConfigureAwait(true)).ConfigureAwait(true);
            }
            finally
            {
                _pauseManager.ResumeActivity(PauseReasons.CardIsLoading);
            }
        }

        async Task CreateAndShowWindowAsync(IReadOnlyCollection<TranslationInfo> translationInfos)
        {
            _logger.LogTrace("Creating window...");
            var window = await _scopedWindowProvider.GetScopedWindowAsync<IAssessmentBatchCardWindow, IReadOnlyCollection<TranslationInfo>>(translationInfos, CancellationToken.None)
                .ConfigureAwait(false);

            window.Closed += Window_Closed;
            _hasOpenWindows = true;

            // CultureUtilities.ChangeCulture(LocalSettingsRepository.UiLanguage);
            _synchronizationContext.Send(
                _ =>
                {
                    _windowPositionAdjustmentManager.AdjustAnyWindowPosition(window);
                    window.Restore();
                },
                null);
            _logger.LogInformation("Window has been opened");
        }

        async IAsyncEnumerable<TranslationInfo> GetTranslationInfosForAllWordsAsync(IEnumerable<LearningInfo> mostSuitableLearningInfos)
        {
            foreach (var learningInfo in mostSuitableLearningInfos)
            {
                var translationEntry = _translationEntryRepository.GetById(learningInfo.Id);
                var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                    .ConfigureAwait(false);
                yield return new TranslationInfo(translationEntry, translationDetails, learningInfo);
                UpdateShowCount(learningInfo);
            }
        }

        void UpdateShowCount(LearningInfo learningInfo)
        {
            learningInfo.ShowCount++; // single place to update show count - no need to synchronize
            learningInfo.LastCardShowTime = DateTimeOffset.Now;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(learningInfo);
        }

        void HandlePauseReasonChanged(PauseReasonAndState pauseReasonAndState)
        {
            if (pauseReasonAndState.IsPaused)
            {
                lock (_lockObject)
                {
                    _interval.Dispose();
                }
            }
            else
            {
                CreateInterval();
            }
        }

        IDisposable ProvideInterval(TimeSpan delay)
        {
            return Observable.Timer(delay, CardShowFrequency).Subscribe(HandleIntervalHitAsync);
        }

        void Window_Closed(object? sender, EventArgs e)
        {
            _ = sender ?? throw new ArgumentNullException(nameof(sender));
            _hasOpenWindows = false;
            ((IDisplayable)sender).Closed -= Window_Closed;
        }
    }
}
