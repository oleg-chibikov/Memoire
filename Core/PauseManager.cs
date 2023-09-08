using System;
using System.Collections.Generic;
using System.Linq;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.Local;
using Mémoire.Contracts.DAL.Model;
using Mémoire.Resources;
using Microsoft.Extensions.Logging;

namespace Mémoire.Core
{
    public sealed class PauseManager : IPauseManager, IDisposable
    {
        static readonly IDictionary<PauseReason, string> PauseReasonLabels = new Dictionary<PauseReason, string>
        {
            { PauseReason.InactiveMode, Texts.PauseReasonInactiveMode },
            { PauseReason.ActiveProcessBlacklisted, Texts.PauseReasonActiveProcessBlacklisted },
            { PauseReason.CardIsVisible, Texts.PauseReasonCardIsVisible },
            { PauseReason.OperationInProgress, Texts.PauseReasonOperationInProgress },
        };

        readonly IDictionary<PauseReason, string> _descriptions = new Dictionary<PauseReason, string>();
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly object _lockObject = new ();
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly IDictionary<PauseReason, PauseInfoSummary> _pauseInfos = new Dictionary<PauseReason, PauseInfoSummary>();

        public PauseManager(ILogger<PauseManager> logger, IMessageHub messageHub, ILocalSettingsRepository localSettingsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            ApplyToEveryPauseReason(pauseReason => _pauseInfos[pauseReason] = _localSettingsRepository.GetPauseInfo(pauseReason));
            if (!_localSettingsRepository.IsActive)
            {
                _pauseInfos[PauseReason.InactiveMode].Pause();
            }

            logger.LogDebug("Initialized {Type}", GetType().Name);
        }

        public bool IsPaused
        {
            get
            {
                lock (_lockObject)
                {
                    return _pauseInfos.Values.Any(pauseInfoCollection => pauseInfoCollection.IsPaused());
                }
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                ApplyToEveryPauseReason(pauseReason => _localSettingsRepository.AddOrUpdatePauseInfo(pauseReason, _pauseInfos[pauseReason]));
            }
        }

        public PauseInfoSummary GetPauseInfo(PauseReason pauseReason)
        {
            lock (_lockObject)
            {
                return _pauseInfos[pauseReason];
            }
        }

        public string? GetPauseReasons()
        {
            lock (_lockObject)
            {
                if (!IsPaused)
                {
                    return null;
                }

                var reasons = _pauseInfos.Where(pauseInfo => pauseInfo.Value.IsPaused())
                    .Select(
                        pauseInfo =>
                        {
                            var text = PauseReasonLabels[pauseInfo.Key];
                            if (_descriptions.TryGetValue(pauseInfo.Key, out var description))
                            {
                                text += $": {description}";
                            }

                            return text;
                        });

                return string.Join(Environment.NewLine, reasons);
            }
        }

        public void PauseActivity(PauseReason pauseReason, string? description)
        {
            bool previousIsPaused;
            bool isPaused;
            lock (_lockObject)
            {
                previousIsPaused = IsPaused;
                if (description != null)
                {
                    _descriptions[pauseReason] = description;
                }

                if (!_pauseInfos[pauseReason].Pause())
                {
                    return;
                }

                isPaused = IsPaused;

                _logger.LogInformation("Paused: {PauseReasons}", pauseReason);
            }

            _messageHub.Publish(new PauseReasonAndState(pauseReason, isPaused));
            if (!previousIsPaused && isPaused)
            {
                _messageHub.Publish(new PauseState(true));
            }
        }

        public void ResumeActivity(PauseReason pauseReason)
        {
            bool previousIsPaused;
            bool isPaused;
            lock (_lockObject)
            {
                previousIsPaused = IsPaused;
                if (!_pauseInfos[pauseReason].Resume())
                {
                    return;
                }

                _descriptions.Remove(pauseReason);
                _localSettingsRepository.AddOrUpdatePauseInfo(pauseReason, _pauseInfos[pauseReason]);
                isPaused = IsPaused;
                _logger.LogInformation("Resumed: {PauseReasons}", pauseReason);
            }

            _messageHub.Publish(new PauseReasonAndState(pauseReason, isPaused));
            if (previousIsPaused && !isPaused)
            {
                _messageHub.Publish(new PauseState(false));
            }
        }

        public void ResetPauseTimes()
        {
            lock (_lockObject)
            {
                ApplyToEveryPauseReason(
                    pauseReason =>
                    {
                        _pauseInfos[pauseReason].Clear();
                        _localSettingsRepository.AddOrUpdatePauseInfo(pauseReason, null);
                    });
            }

            _logger.LogDebug("Paused time is reset");
        }

        static void ApplyToEveryPauseReason(Action<PauseReason> action)
        {
            foreach (var pauseReason in Enum.GetValues(typeof(PauseReason)).Cast<PauseReason>().Where(p => p != PauseReason.None))
            {
                action(pauseReason);
            }
        }
    }
}
