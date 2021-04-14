using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Easy.MessageHub;
using Mémoire.Contracts;
using Mémoire.Contracts.DAL.SharedBetweenMachines;
using Mémoire.Resources;
using NAudio.Wave;
using Scar.Common.Messages;
using Scar.Services.Contracts;

namespace Mémoire.Core
{
    sealed class TextToSpeechPlayerWrapper : ITextToSpeechPlayerWrapper
    {
        readonly ITextToSpeechPlayer _textToSpeechPlayer;
        readonly ISharedSettingsRepository _sharedSettingsRepository;
        readonly IMessageHub _messageHub;

        public TextToSpeechPlayerWrapper(ITextToSpeechPlayer textToSpeechPlayer, ISharedSettingsRepository sharedSettingsRepository, IMessageHub messageHub)
        {
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _sharedSettingsRepository = sharedSettingsRepository ?? throw new ArgumentNullException(nameof(sharedSettingsRepository));
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
        }

        public async Task PlayTtsAsync(string text, string language, CancellationToken cancellationToken)
        {
            await _textToSpeechPlayer.PlayTtsAsync(
                    text,
                    Speak,
                    language,
                    _sharedSettingsRepository.TtsSpeaker,
                    _sharedSettingsRepository.TtsVoiceEmotion,
                    ex => _messageHub.Publish(Errors.CannotSpeak.ToError(ex)),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        void Speak(Stream soundStream)
        {
            using var reset = new AutoResetEvent(false);
            using var waveOut = new WaveOutEvent();
            using var reader = new Mp3FileReader(soundStream);
            waveOut.Init(reader);

            // Thread should be alive while playback is not stopped
            void PlaybackStoppedHandler(object? sender, StoppedEventArgs e)
            {
                _ = sender ?? throw new ArgumentNullException(nameof(sender));

                ((WaveOutEvent)sender).PlaybackStopped -= PlaybackStoppedHandler;

                // ReSharper disable once AccessToDisposedClosure - dispose happens only after this call is awaited
                reset!.Set();
            }

            waveOut.PlaybackStopped += PlaybackStoppedHandler;
            waveOut.Play();

            // Wait for tts to be finished not longer than 5 seconds (if PlaybackStopped is not firing)
            reset.WaitOne(TimeSpan.FromSeconds(5));
        }
    }
}
