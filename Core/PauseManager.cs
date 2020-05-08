using System;
using System.Collections.Generic;
using System.Linq;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Resources;

namespace Remembrance.Core
{
    sealed class PauseManager : IPauseManager, IDisposable
    {
        static readonly IDictionary<PauseReasons, string> PauseReasonLabels = new Dictionary<PauseReasons, string>
        {
            { PauseReasons.InactiveMode, Texts.PauseReasonInactiveMode },
            { PauseReasons.ActiveProcessBlacklisted, Texts.PauseReasonActiveProcessBlacklisted },
            { PauseReasons.CardIsVisible, Texts.PauseReasonCardIsVisible },
            { PauseReasons.OperationInProgress, Texts.PauseReasonOperationInProgress }
        };

        readonly IDictionary<PauseReasons, string> _descriptions = new Dictionary<PauseReasons, string>();

        readonly ILocalSettingsRepository _localSettingsRepository;

        readonly object _lockObject = new object();

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        readonly IDictionary<PauseReasons, PauseInfoCollection> _pauseInfos = new Dictionary<PauseReasons, PauseInfoCollection>();

        public PauseManager(ILogger<PauseManager> logger, IMessageHub messageHub, ILocalSettingsRepository localSettingsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            ApplyToEveryPauseReason(pauseReason => _pauseInfos[pauseReason] = _localSettingsRepository.GetPauseInfo(pauseReason));
            if (!_localSettingsRepository.IsActive)
            {
                _pauseInfos[PauseReasons.InactiveMode].Pause();
            }
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

        public PauseInfoCollection GetPauseInfo(PauseReasons pauseReasons)
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
                            if (_descriptions.ContainsKey(pauseInfo.Key))
                            {
                                text += ": " + _descriptions[pauseInfo.Key];
                            }

                            return text;
                        });

                return string.Join(Environment.NewLine, reasons);
            }
        }

        public void PauseActivity(PauseReasons pauseReasons, string? description)
        {
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

                _logger.LogInformation($"Paused: {pauseReasons}");
            }

            _messageHub.Publish(pauseReasons);
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

        public void ResumeActivity(PauseReasons pauseReasons)
        {
            lock (_lockObject)
            {
                if (!_pauseInfos[pauseReasons].Resume())
                {
                    return;
                }

                _descriptions.Remove(pauseReasons);
                _localSettingsRepository.AddOrUpdatePauseInfo(pauseReasons, _pauseInfos[pauseReasons]);
                _logger.LogInformation($"Resumed: {pauseReasons}");
            }

            _messageHub.Publish(pauseReasons);
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
