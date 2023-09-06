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
        static readonly IDictionary<PauseReasons, string> PauseReasonLabels = new Dictionary<PauseReasons, string>
        {
            { PauseReasons.InactiveMode, Texts.PauseReasonInactiveMode },
            { PauseReasons.ActiveProcessBlacklisted, Texts.PauseReasonActiveProcessBlacklisted },
            { PauseReasons.CardIsVisible, Texts.PauseReasonCardIsVisible },
            { PauseReasons.OperationInProgress, Texts.PauseReasonOperationInProgress },
            { PauseReasons.CardIsLoading, Texts.PauseReasonCardIsLoading }
        };

        readonly IDictionary<PauseReasons, string> _descriptions = new Dictionary<PauseReasons, string>();
        readonly ILocalSettingsRepository _localSettingsRepository;
        readonly object _lockObject = new ();
        readonly ILogger _logger;
        readonly IMessageHub _messageHub;
        readonly IDictionary<PauseReasons, PauseInfoSummary> _pauseInfos = new Dictionary<PauseReasons, PauseInfoSummary>();

        public PauseManager(ILogger<PauseManager> logger, IMessageHub messageHub, ILocalSettingsRepository localSettingsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.LogTrace("Initializing {Type}...", GetType().Name);
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            ApplyToEveryPauseReason(pauseReason => _pauseInfos[pauseReason] = _localSettingsRepository.GetPauseInfo(pauseReason));
            if (!_localSettingsRepository.IsActive)
            {
                _pauseInfos[PauseReasons.InactiveMode].Pause();
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

        public PauseInfoSummary GetPauseInfo(PauseReasons pauseReasons)
        {
            lock (_lockObject)
            {
                return _pauseInfos[pauseReasons];
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

        public void PauseActivity(PauseReasons pauseReasons, string? description)
        {
            bool isPaused;
            lock (_lockObject)
            {
                if (description != null)
                {
                    _descriptions[pauseReasons] = description;
                }

                if (!_pauseInfos[pauseReasons].Pause())
                {
                    return;
                }

                isPaused = IsPaused;

                _logger.LogInformation("Paused: {PauseReasons}", pauseReasons);
            }

            _messageHub.Publish(new PauseReasonAndState(pauseReasons, isPaused));
        }

        public void ResumeActivity(PauseReasons pauseReasons)
        {
            bool isPaused;
            lock (_lockObject)
            {
                if (!_pauseInfos[pauseReasons].Resume())
                {
                    return;
                }

                _descriptions.Remove(pauseReasons);
                _localSettingsRepository.AddOrUpdatePauseInfo(pauseReasons, _pauseInfos[pauseReasons]);
                isPaused = IsPaused;
                _logger.LogInformation("Resumed: {PauseReasons}", pauseReasons);
            }

            _messageHub.Publish(new PauseReasonAndState(pauseReasons, isPaused));
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

        static void ApplyToEveryPauseReason(Action<PauseReasons> action)
        {
            foreach (var pauseReason in Enum.GetValues(typeof(PauseReasons)).Cast<PauseReasons>().Where(p => p != PauseReasons.None))
            {
                action(pauseReason);
            }
        }
    }
}
