using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Contracts.DAL.Model;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Remembrance.Contracts.View.Card;
using Scar.Common.WPF.View;
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
        private readonly object _lockObject = new object();

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly IPauseManager _pauseManager;

        [NotNull]
        private readonly IScopedWindowProvider _scopedWindowProvider;

        [NotNull]
        private readonly IList<Guid> _subscriptionTokens = new List<Guid>();

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        [NotNull]
        private readonly ITranslationEntryRepository _translationEntryRepository;

        private bool _hasOpenWindows;

        [NotNull]
        private IDisposable _interval;

        public AssessmentCardManager(
            [NotNull] ITranslationEntryRepository translationEntryRepository,
            [NotNull] ILocalSettingsRepository localSettingsRepository,
            [NotNull] ILog logger,
            [NotNull] IMessageHub messageHub,
            [NotNull] ITranslationEntryProcessor translationEntryProcessor,
            [NotNull] ISettingsRepository settingsRepository,
            [NotNull] SynchronizationContext synchronizationContext,
            [NotNull] ILearningInfoRepository learningInfoRepository,
            [NotNull] IScopedWindowProvider scopedWindowProvider,
            [NotNull] IPauseManager pauseManager)
            : base(localSettingsRepository, logger, synchronizationContext)
        {
            logger.Trace("Starting showing cards...");
            _ = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            // just to assign the field in the constructor
            _interval = Disposable.Empty;
            _scopedWindowProvider = scopedWindowProvider ?? throw new ArgumentNullException(nameof(scopedWindowProvider));
            _pauseManager = pauseManager ?? throw new ArgumentNullException(nameof(pauseManager));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
            _learningInfoRepository = learningInfoRepository ?? throw new ArgumentNullException(nameof(learningInfoRepository));
            _translationEntryRepository = translationEntryRepository ?? throw new ArgumentNullException(nameof(translationEntryRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _initTime = DateTime.Now;
            CardShowFrequency = settingsRepository.CardShowFrequency;
            LastCardShowTime = localSettingsRepository.LastCardShowTime;
            if (!_pauseManager.IsPaused)
            {
                CreateInterval();
            }

            _subscriptionTokens.Add(messageHub.Subscribe<TimeSpan>(OnCardShowFrequencyChanged));
            _subscriptionTokens.Add(_messageHub.Subscribe<PauseReason>(OnPauseReasonChanged));
            logger.Debug("Started showing cards");
        }

        public TimeSpan CardShowFrequency { get; private set; }

        public DateTime? LastCardShowTime { get; private set; }

        public DateTime NextCardShowTime => DateTime.Now + TimeLeftToShowCard;

        public TimeSpan TimeLeftToShowCard
        {
            get
            {
                var requiredInterval = CardShowFrequency + _pauseManager.GetPauseInfo(PauseReason.CardIsVisible).GetPauseTime();
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

        protected override async Task<IWindow> TryCreateWindowAsync(TranslationInfo translationInfo, IWindow ownerWindow)
        {
            if (_hasOpenWindows)
            {
                Logger.Trace("There is another window opened. Skipping creation...");
                return null;
            }

            Logger.TraceFormat("Creating window for {0}...", translationInfo);
            var learningInfo = translationInfo.LearningInfo;
            IWindow window;
            switch (learningInfo.RepeatType)
            {
                case RepeatType.Elementary:
                case RepeatType.Beginner:
                case RepeatType.Novice:
                {
                    // ReSharper disable once StyleCop.SA1009
                    window = await _scopedWindowProvider
                        .GetScopedWindowAsync<IAssessmentViewOnlyCardWindow, (IWindow, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                        .ConfigureAwait(false);
                    break;
                }

                case RepeatType.PreIntermediate:
                case RepeatType.Intermediate:
                case RepeatType.UpperIntermediate:
                {
                    // ReSharper disable once StyleCop.SA1009
                    window = await _scopedWindowProvider
                        .GetScopedWindowAsync<IAssessmentTextInputCardWindow, (IWindow, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                        .ConfigureAwait(false);
                    break;
                }

                case RepeatType.Advanced:
                case RepeatType.Proficiency:
                case RepeatType.Expert:
                {
                    // ReSharper disable once StyleCop.SA1009
                    window = await _scopedWindowProvider
                        .GetScopedWindowAsync<IAssessmentTextInputCardWindow, (IWindow, TranslationInfo)>((ownerWindow, translationInfo), CancellationToken.None)
                        .ConfigureAwait(false);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            window.Closed += Window_Closed;
            _hasOpenWindows = true;
            learningInfo.ShowCount++; // single place to update show count - no need to synchronize
            learningInfo.LastCardShowTime = DateTime.Now;
            _learningInfoRepository.Update(learningInfo);
            _messageHub.Publish(learningInfo);
            return window;
        }

        private void CreateInterval()
        {
            var delay = TimeLeftToShowCard;
            Logger.DebugFormat("Next card will be shown in: {0} (frequency is {1})", delay, CardShowFrequency);

            lock (_lockObject)
            {
                _interval.Dispose();
                _interval = ProvideInterval(delay);
            }
        }

        private void OnCardShowFrequencyChanged(TimeSpan newCardShowFrequency)
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

        private async void OnIntervalHit(long x)
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            Logger.Trace("Trying to show next card...");

            if (!LocalSettingsRepository.IsActive)
            {
                Logger.Debug("Skipped showing card due to inactivity");
                return;
            }

            _pauseManager.ResetPauseTimes();

            LocalSettingsRepository.LastCardShowTime = LastCardShowTime = DateTime.Now;
            var mostSuitableLearningInfo = _learningInfoRepository.GetMostSuitable();
            if (mostSuitableLearningInfo == null)
            {
                Logger.Debug("Skipped showing card due to absence of suitable cards");
                return;
            }

            var translationEntry = _translationEntryRepository.GetById(mostSuitableLearningInfo.Id);

            var translationDetails = await _translationEntryProcessor
                .ReloadTranslationDetailsIfNeededAsync(translationEntry.Id, translationEntry.ManualTranslations, CancellationToken.None)
                .ConfigureAwait(false);
            var translationInfo = new TranslationInfo(translationEntry, translationDetails, mostSuitableLearningInfo);
            Logger.TraceFormat("Trying to show {0}...", translationInfo);
            await ShowCardAsync(translationInfo, null).ConfigureAwait(false);
        }

        private void OnPauseReasonChanged(PauseReason pauseReason)
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

        [NotNull]
        private IDisposable ProvideInterval(TimeSpan delay)
        {
            return Observable.Timer(delay, CardShowFrequency).Subscribe(OnIntervalHit);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _hasOpenWindows = false;
            ((Window)sender).Closed -= Window_Closed;
        }
    }
}