using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.SharedBetweenMachines;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Translation.Yandex
{
    sealed class TextToSpeechPlayer : ITextToSpeechPlayer, IDisposable
    {
        const string ApiKey = "e07b8971-5fcd-477a-b141-c8620e7f06eb";

        static readonly Regex CyrillicRegex = new Regex("[а-яА-ЯёЁ]+", RegexOptions.Compiled);

        readonly HttpClient _httpClient = new HttpClient { BaseAddress = new Uri("https://tts.voicetech.yandex.net/") };

        readonly ILogger _logger;

        readonly IMessageHub _messageHub;

        readonly ISharedSettingsRepository _sharedSettingsRepository;

        public TextToSpeechPlayer(ILogger<TextToSpeechPlayer> logger, ISharedSettingsRepository sharedSettingsRepository, IMessageHub messageHub)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception handler")]
        public async Task<bool> PlayTtsAsync(string text, string lang, CancellationToken cancellationToken)
        {
            _ = text ?? throw new ArgumentNullException(nameof(text));
            _ = lang ?? throw new ArgumentNullException(nameof(lang));
            _logger.LogTrace("Starting speaking {0}...", text);
            using var reset = new AutoResetEvent(false);
            var uriPart =
                $"generate?text={text}&format={Format.Mp3.ToString().ToLowerInvariant()}&lang={PrepareLanguage(lang, text)}&speaker={_sharedSettingsRepository.TtsSpeaker.ToString().ToLowerInvariant()}&emotion={_sharedSettingsRepository.TtsVoiceEmotion.ToString().ToLowerInvariant()}&key={ApiKey}";
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var soundStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var waveOut = new WaveOutEvent();
                using var reader = new Mp3FileReader(soundStream);
                waveOut.Init(reader);

                // Thread should be alive while playback is not stopped
                void PlaybackStoppedHandler(object s, StoppedEventArgs e)
                {
                    ((WaveOutEvent)s).PlaybackStopped -= PlaybackStoppedHandler;
                    reset.Set();
                }

                waveOut.PlaybackStopped += PlaybackStoppedHandler;
                waveOut.Play();

                // Wait for tts to be finished not longer than 5 seconds (if PlaybackStopped is not firing)
                reset.WaitOne(TimeSpan.FromSeconds(5));

                _logger.LogDebug("Finished speaking {0}", text);
                return true;
            }
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotSpeak.ToError(ex));
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        static string PrepareLanguage(string lang, string text)
        {
            switch (lang)
            {
                case Constants.RuLanguageTwoLetters: // russian
                case "uk": // ukrainian
                case "tr": // turkish
                    return lang;
                default:
                    return CyrillicRegex.IsMatch(text) ? Constants.RuLanguageTwoLetters : Constants.EnLanguageTwoLetters;
            }
        }
    }
}
