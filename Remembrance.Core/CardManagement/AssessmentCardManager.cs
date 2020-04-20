using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common;
using Scar.Common.View.Contracts;

namespace Remembrance.Core.CardManagement
{
    sealed class AssessmentCardManager : BaseCardManager, IAssessmentCardManager, ICardShowTimeProvider, IDisposable
    {
        readonly DateTime _initTime;

        readonly ILearningInfoRepository _learningInfoRepository;

        readonly ILocalSettingsRepository _localSettingsRepository;

        readonly ISharedSettingsRepository _sharedSettingsRepository;

        readonly object _lockObject = new object();

        readonly IMessageHub _messageHub;

        readonly IPauseManager _pauseManager;

        readonly IScopedWindowProvider _scopedWindowProvider;

        readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        readonly ITranslationEntryProcessor _translationEntryProcessor;

        readonly ITranslationEntryRepository _translationEntryRepository;

        bool _hasOpenWindows;

        IDisposable _interval = Disposable.Empty;

        public AssessmentCardManager(
            ITranslationEntryRepository translationEntryRepository,
            ILog logger,
            IMessageHub messageHub,
            ITranslationEntryProcessor translationEntryProcessor,
            ILocalSettingsRepository localSettingsRepository,
            SynchronizationContext synchronizationContext,
            ILearningInfoRepository learningInfoRepository,
            IScopedWindowProvider scopedWindowProvider,
            IPauseManager pauseManager,
            IWindowPositionAdjustmentManager windowPositionAdjustmentManager,
            ISharedSettingsRepository sharedSettingsRepository) : base(logger, synchronizationContext, windowPositionAdjustmentManager)
        {
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _ = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _localSettingsRepository = localSettingsRepository;
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _initTime = DateTime.Now;
            CardShowFrequency = sharedSettingsRepository.CardShowFrequency;
            LastCardShowTime = localSettingsRepository.LastCardShowTime;

            logger.Trace("Starting showing cards...");
            if (!_pauseManager.IsPaused)
            {
                CreateInterval();
            }

            _subscriptionTokens.Add(messageHub.Subscribe<TimeSpan>(HandleCardShowFrequencyChanged));
            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReasons>(HandlePauseReasonChanged));
            logger.Debug("Started showing cards");
        }

        public TimeSpan CardShowFrequency { get; private set; }

        public DateTime? LastCardShowTime { get; private set; }

        public DateTime NextCardShowTime => DateTime.Now + TimeLeftToShowCard;

        public TimeSpan TimeLeftToShowCard
        {
            get
            {
                var requiredInterval = CardShowFrequency + _pauseManager.GetPauseInfo(PauseReasons.CardIsVisible).GetPauseTime();
                var alreadyPassedTime = DateTime.Now - (LastCardShowTime ?? _initTime);
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

            Logger.Debug("Finished showing cards");
        }

        void CreateInterval()
        {
            var delay = TimeLeftToShowCard;
            Logger.DebugFormat("Next card will be shown in: {0} (frequency is {1})", delay, CardShowFrequency);

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
                Logger.TraceFormat("Recreating interval for new frequency {0}...", newCardShowFrequency);
                lock (_lockObject)
                {
                    CreateInterval();
                }
            }
            else
            {
                Logger.DebugFormat("Skipped recreating interval for new frequency {0} as it is paused", newCardShowFrequency);
            }
        }

        async void HandleIntervalHit(long x)
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            Logger.Trace("Trying to show next card...");

            if (!_localSettingsRepository.IsActive)
            {
                Logger.Debug("Skipped showing card due to inactivity");
                return;
            }

            if (_hasOpenWindows)
            {
                Logger.Trace("There is another window opened. Skipping creation...");
                return;
            }

            _pauseManager.ResetPauseTimes();

            _localSettingsRepository.LastCardShowTime = LastCardShowTime = DateTime.Now;
            var mostSuitableLearningInfos = _learningInfoRepository.GetMostSuitable(_sharedSettingsRepository.CardsToShowAtOnce).ToArray();
            if (mostSuitableLearningInfos.Length == 0)
            {
                Logger.Debug("Skipped showing card due to absence of suitable cards");
                return;
            }

            IReadOnlyCollection<TranslationInfo> translationInfos;
            try
            {
                translationInfos = await GetTranslationInfosForAllWords(mostSuitableLearningInfos).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                // If the word cannot be found in DB // TODO: additional handling? get it from somewhere
                _messageHub.Publish(ex);
                return;
            }

            await CreateAndShowWindowAsync(translationInfos).ConfigureAwait(false);
        }

        async Task CreateAndShowWindowAsync(IReadOnlyCollection<TranslationInfo> translationInfos)
        {
            Logger.TraceFormat("Creating window...");
            var window = await _scopedWindowProvider.GetScopedWindowAsync<IAssessmentBatchCardWindow, IReadOnlyCollection<TranslationInfo>>(translationInfos, CancellationToken.None)
                .ConfigureAwait(false);

            window.Closed += Window_Closed;
            _hasOpenWindows = true;
            ShowWindow(window);
            Logger.InfoFormat("Window has been opened");
        }

        async Task<IReadOnlyCollection<TranslationInfo>> GetTranslationInfosForAllWords(IEnumerable<LearningInfo> mostSuitableLearningInfos)
        {
            // TODO: would be good to use AsyncEnumerable (but is it supported for .net framework?)
            var translationInfos = new List<TranslationInfo>();
            foreach (var learningInfo in mostSuitableLearningInfos)
            {
                var translationEntry = _translationEntryRepository.GetById(learningInfo.Id);
                var translationDetails = await _translationEntryProcessor.ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                    .ConfigureAwait(false);
                translationInfos.Add(new TranslationInfo(translationEntry, translationDetails, learningInfo));
                UpdateShowCount(learningInfo);
            }

            return translationInfos;
        }

        void UpdateShowCount(LearningInfo learningInfo)
        {
            learningInfo.ShowCount++; // single place to update show count - no need to synchronize
            learningInfo.LastCardShowTime = DateTime.Now;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(learningInfo);
        }

        void HandlePauseReasonChanged(PauseReasons pauseReasons)
        {
            if (_pauseManager.IsPaused)
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
            return Observable.Timer(delay, CardShowFrequency).Subscribe(HandleIntervalHit);
        }

        void Window_Closed(object sender, EventArgs e)
        {
            _hasOpenWindows = false;
            ((IDisplayable)sender).Closed -= Window_Closed;
        }
    }
}
