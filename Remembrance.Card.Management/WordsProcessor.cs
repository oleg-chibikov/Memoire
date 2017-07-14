using System;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.DAL.Contracts.Model;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Interfaces;
using Scar.Common.Exceptions;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class WordsProcessor : IWordsProcessor
    {
        [NotNull]
        private readonly ITranslationResultCardManager _cardManager;

        [NotNull]
        private readonly ILog _logger;

        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly ITextToSpeechPlayer _textToSpeechPlayer;

        [NotNull]
        private readonly IWordsAdder _wordsAdder;

        public WordsProcessor(
            [NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IWordsAdder wordsAdder,
            [NotNull] ILog logger,
            [NotNull] ITranslationResultCardManager cardManager,
            [NotNull] IMessenger messenger)
        {
            _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
            _wordsAdder = wordsAdder ?? throw new ArgumentNullException(nameof(wordsAdder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cardManager = cardManager ?? throw new ArgumentNullException(nameof(cardManager));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public bool ProcessNewWord(string word, string sourceLanguage, string targetLanguage, bool showCard)
        {
            _logger.Info($"Processing word {word}...");
            if (word == null)
                throw new ArgumentNullException(nameof(word));

            TranslationInfo translationInfo;
            try
            {
                translationInfo = _wordsAdder.AddWordWithChecks(word, sourceLanguage, targetLanguage, true);
            }
            catch (LocalizableException ex)
            {
                _logger.Warn(ex.Message);
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                _logger.Warn($"Word {word} has not been processed");
                return false;
            }

            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                _cardManager.ShowCard(translationInfo);
            _logger.Trace($"Word {word} has been processed");
            return true;
        }

        public bool ChangeText(int id, string newWord, string sourceLanguage, string targetLanguage, bool showCard)
        {
            _logger.Info($"Changing text for {newWord} for word {id}...");
            if (newWord == null)
                throw new ArgumentNullException(nameof(newWord));
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));
            if (targetLanguage == null)
                throw new ArgumentNullException(nameof(targetLanguage));

            TranslationInfo translationInfo;
            try
            {
                translationInfo = _wordsAdder.AddWordWithChecks(newWord, sourceLanguage, targetLanguage, true, id);
            }
            catch (LocalizableException ex)
            {
                _logger.Warn(ex.Message);
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                _logger.Warn($"Text was not changed for word {id}");
                return false;
            }

            _textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            _messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                _cardManager.ShowCard(translationInfo);
            _logger.Trace($"Text has been changed for word {id}");
            return true;
        }
    }
}