using System;
using Common.Logging;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;
using Remembrance.Resources;
using Remembrance.Translate.Contracts.Interfaces;

namespace Remembrance.Card.Management
{
    [UsedImplicitly]
    internal sealed class WordsAdder : IWordsAdder
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
        private readonly IWordsChecker wordsChecker;

        public WordsAdder([NotNull] ITextToSpeechPlayer textToSpeechPlayer,
            [NotNull] IWordsChecker wordsChecker,
            [NotNull] ILog logger,
            [NotNull] ITranslationResultCardManager cardManager,
            [NotNull] IMessenger messenger)
        {
            if (textToSpeechPlayer == null)
                throw new ArgumentNullException(nameof(textToSpeechPlayer));
            if (wordsChecker == null)
                throw new ArgumentNullException(nameof(wordsChecker));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (cardManager == null)
                throw new ArgumentNullException(nameof(cardManager));
            if (messenger == null)
                throw new ArgumentNullException(nameof(messenger));

            this.textToSpeechPlayer = textToSpeechPlayer;
            this.wordsChecker = wordsChecker;
            this.logger = logger;
            this.cardManager = cardManager;
            this.messenger = messenger;
        }

        public void AddWord(string word, string sourceLanguage = null, string targetLanguage = null)
        {
            if (word == null)
                throw new ArgumentNullException(nameof(word));
            logger.Info($"Adding word {word}...");

            var translationInfo = wordsChecker.CheckWord(word, sourceLanguage, targetLanguage, true);

            textToSpeechPlayer.PlayTtsAsync(translationInfo.Key.Text, translationInfo.Key.SourceLanguage);

            messenger.Send(translationInfo, MessengerTokens.TranslationInfoToken);

            cardManager.ShowCard(translationInfo);
            logger.Debug($"Word {word} has been added");
        }
    }
}