using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;
using NAudio.Wave;
using Remembrance.Contracts.DAL;
using Remembrance.Contracts.Translate;
using Remembrance.Contracts.Translate.Data.TextToSpeechPlayer;
using Remembrance.Resources;

namespace Remembrance.Core.Translation.Yandex
{
    [UsedImplicitly]
    internal sealed class TextToSpeechPlayer : ITextToSpeechPlayer
    {
        [NotNull]
        private const string ApiKey = "e07b8971-5fcd-477a-b141-c8620e7f06eb";

        [NotNull]
        private static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri("https://tts.voicetech.yandex.net/")
        };

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly ISettingsRepository _settingsRepository;

        public TextToSpeechPlayer([NotNull] ILog logger, [NotNull] ISettingsRepository settingsRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
        }

        public async Task<bool> PlayTtsAsync(string text, string lang, CancellationToken cancellationToken)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (lang == null)
                throw new ArgumentNullException(nameof(lang));

            _logger.Trace($"Starting speaking {text}...");
            var settings = _settingsRepository.Get();
            var reset = new AutoResetEvent(false);
            var uriPart =
                $"generate?text={text}&format={Format.Mp3.ToString().ToLowerInvariant()}&lang={PrepareLanguage(lang)}&speaker={settings.TtsSpeaker.ToString().ToLowerInvariant()}&emotion={settings.TtsVoiceEmotion.ToString().ToLowerInvariant()}&key={ApiKey}";
            var response = await Client.GetAsync(uriPart, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return false;

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

            _logger.Trace($"Finished speaking {text}");
            return true;
        }

        [NotNull]
        private static string PrepareLanguage([NotNull] string lang)
        {
            switch (lang)
            {
                case Constants.RuLanguageTwoLetters:
                    return lang;
                default:
                    return Constants.EnLanguageTwoLetters;
            }
        }
    }
}