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
        private readonly ITranslationResultCardManager cardManager;

        [NotNull]
        private readonly ILog logger;

        [NotNull]
        private readonly IMessenger messenger;

        [NotNull]
        private readonly ITextToSpeechPlayer textToSpeechPlayer;

        [NotNull]
        private readonly IWordsAdder wordsAdder;

        public WordsProcessor([NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IWordsAdder wordsAdder,
            [NotNull] ILog logger,
            [NotNull] ITranslationResultCardManager cardManager,
            [NotNull] IMessenger messenger)
        {
            if (textToSpeechPlayer == null)
                throw new ArgumentNullException(nameof(textToSpeechPlayer));
            if (wordsAdder == null)
                throw new ArgumentNullException(nameof(wordsAdder));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (cardManager == null)
                throw new ArgumentNullException(nameof(cardManager));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            this.textToSpeechPlayer = textToSpeechPlayer;
            this.wordsAdder = wordsAdder;
            this.logger = logger;
            this.cardManager = cardManager;
            this.messenger = messenger;
        }

        public bool ProcessNewWord(string word, string sourceLanguage, string targetLanguage, bool showCard)
        {
            logger.Info($"Processing word {word}...");
            if (word == null)
                throw new ArgumentNullException(nameof(word));
            TranslationInfo translationInfo;
            try
            {
                translationInfo = wordsAdder.AddWordWithChecks(word, sourceLanguage, targetLanguage, true);
            }
            catch (LocalizableException ex)
            {
                logger.Warn(ex.Message);
                messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                logger.Warn($"Word {word} has not been processed");
                return false;
            }

            textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                cardManager.ShowCard(translationInfo);
            logger.Debug($"Word {word} has been processed");
            return true;
        }

        public bool ChangeText(int id, string newWord, string sourceLanguage, string targetLanguage, bool showCard)
        {
            logger.Info($"Changing text for {newWord} for word {id}...");
            if (newWord == null)
                throw new ArgumentNullException(nameof(newWord));
            if (sourceLanguage == null)
                throw new ArgumentNullException(nameof(sourceLanguage));
            if (targetLanguage == null)
                throw new ArgumentNullException(nameof(targetLanguage));
            TranslationInfo translationInfo;
            try
            {
                translationInfo = wordsAdder.AddWordWithChecks(newWord, sourceLanguage, targetLanguage, true, id);
            }
            catch (LocalizableException ex)
            {
                logger.Warn(ex.Message);
                messenger.Send(ex.LocalizedMessage, MessengerTokens.UserMessageToken);
                logger.Warn($"Text was not changed for word {id}");
                return false;
            }

            textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);
            if (showCard)
                cardManager.ShowCard(translationInfo);
            logger.Debug($"Text has been changed for word {id}");
            return true;
        }
    }
}