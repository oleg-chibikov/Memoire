using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Easy.MessageHub;
using JetBrains.Annotations;
using NAudio.Wave;
using Remembrance.Contracts.DAL.Shared;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;
using Scar.Common.Messages;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class TextToSpeechPlayer : ITextToSpeechPlayer
    {
        [NotNull]
        private const string ApiKey = "e07b8971-5fcd-477a-b141-c8620e7f06eb";

        private static readonly Regex CyryllicRegex = new Regex("[а-яА-ЯёЁ]+", RegexOptions.Compiled);

        [NotNull]
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://tts.voicetech.yandex.net/")
        };

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        public TextToSpeechPlayer([NotNull] ILog logger, [NotNull] ISettingsRepository settingsRepository, [NotNull] IMessageHub messageHub)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task<bool> PlayTtsAsync(string text, string lang, CancellationToken cancellationToken)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (lang == null)
            {
                throw new ArgumentNullException(nameof(lang));
            }

            _logger.TraceFormat("Starting speaking {0}...", text);
            var reset = new AutoResetEvent(false);
            var uriPart =
                $"generate?text={text}&format={Format.Mp3.ToString().ToLowerInvariant()}&lang={PrepareLanguage(lang, text)}&speaker={_settingsRepository.TtsSpeaker.ToString().ToLowerInvariant()}&emotion={_settingsRepository.TtsVoiceEmotion.ToString().ToLowerInvariant()}&key={ApiKey}";
            try
            {
                var response = await _httpClient.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var soundStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using (var waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                using (var reader = new Mp3FileReader(soundStream))
                {
                    waveOut.Init(reader);

                    // Thread should be alive while playback is not stopped
                    void PlaybackStoppedHandler(object s, StoppedEventArgs e)
                    {
                        ((WaveOut)s).PlaybackStopped -= PlaybackStoppedHandler;
                        reset.Set();
                    }

                    waveOut.PlaybackStopped += PlaybackStoppedHandler;
                    waveOut.Play();

                    // Wait for tts to be finished not longer than 5 seconds (if PlaybackStopped is not firing)
                    reset.WaitOne(TimeSpan.FromSeconds(5));
                }

                _logger.DebugFormat("Finished speaking {0}", text);
                return true;
            }
            catch (Exception ex)
            {
                _messageHub.Publish(Errors.CannotSpeak.ToError(ex));
                return false;
            }
        }

        [NotNull]
        private static string PrepareLanguage([NotNull] string lang, [NotNull] string text)
        {
            switch (lang)
            {
                case Constants.RuLanguageTwoLetters: // russian
                case "uk": // ukrainian
                case "tr": // turkish
                    return lang;
                default:
                    return CyryllicRegex.IsMatch(text) ? Constants.RuLanguageTwoLetters : Constants.EnLanguageTwoLetters;
            }
        }
    }
}