using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Easy.MessageHub;
using Remembrance.Contracts.CardManagement.Data;
using Remembrance.Contracts.DAL.Local;
using Remembrance.Resources;

namespace Remembrance.Core
{
    sealed class PauseManager : IPauseManager, IDisposable
    {
        static readonly IDictionary<PauseReason, string> PauseReasonLabels = new Dictionary<PauseReason, string>
        {
            { PauseReason.InactiveMode, Texts.PauseReasonInactiveMode },
            { PauseReason.ActiveProcessBlacklisted, Texts.PauseReasonActiveProcessBlacklisted },
            { PauseReason.CardIsVisible, Texts.PauseReasonCardIsVisible },
            { PauseReason.OperationInProgress, Texts.PauseReasonOperationInProgress }
        };

        readonly IDictionary<PauseReason, string> _descriptions = new Dictionary<PauseReason, string>();

        readonly ILocalSettingsRepository _localSettingsRepository;

        readonly object _lockObject = new object();

        readonly ILog _logger;

        readonly IMessageHub _messageHub;

        readonly IDictionary<PauseReason, PauseInfoCollection> _pauseInfos = new Dictionary<PauseReason, PauseInfoCollection>();

        public PauseManager(ILog logger, IMessageHub messageHub, ILocalSettingsRepository localSettingsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _localSettingsRepository = localSettingsRepository ?? throw new ArgumentNullException(nameof(localSettingsRepository));

            ApplyToEveryPauseReason(pauseReason => _pauseInfos[pauseReason] = _localSettingsRepository.GetPauseInfo(pauseReason));
            if (!_localSettingsRepository.IsActive)
            {
                _pauseInfos[PauseReason.InactiveMode].Pause();
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                ApplyToEveryPauseReason(pauseReason => _localSettingsRepository.AddOrUpdatePauseInfo(pauseReason, _pauseInfos[pauseReason]));
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

        public PauseInfoCollection GetPauseInfo(PauseReason pauseReason)
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
                            if (_descriptions.ContainsKey(pauseInfo.Key))
                            {
                                text += ": " + _descriptions[pauseInfo.Key];
                            }

                            return text;
                        });

                return string.Join(Environment.NewLine, reasons);
            }
        }

        public void Pause(PauseReason pauseReason, string? description)
        {
            lock (_lockObject)
            {
                if (description != null)
                {
                    _descriptions[pauseReason] = description;
                }

                if (!_pauseInfos[pauseReason].Pause())
                {
                    return;
                }

                _logger.Info($"Paused: {pauseReason}");
            }

            _messageHub.Publish(pauseReason);
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

            _logger.Debug("Paused time is resetted");
        }

        public void Resume(PauseReason pauseReason)
        {
            lock (_lockObject)
            {
                if (!_pauseInfos[pauseReason].Resume())
                {
                    return;
                }

                _descriptions.Remove(pauseReason);
                _localSettingsRepository.AddOrUpdatePauseInfo(pauseReason, _pauseInfos[pauseReason]);
                _logger.Info($"Resumed: {pauseReason}");
            }

            _messageHub.Publish(pauseReason);
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