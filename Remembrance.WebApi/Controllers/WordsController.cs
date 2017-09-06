using System;
using System.Web.Http;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Remembrance.Resources;
using Scar.Common.Exceptions;

namespace Remembrance.WebApi.Controllers
{
    [UsedImplicitly]
    public sealed class WordsController : ApiController
    {
        [NotNull]
        private readonly IMessenger _messenger;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public WordsController([NotNull] IWordsProcessor wordsProcessor, [NotNull] IMessenger messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
        }

        [HttpPut]
        [UsedImplicitly]
        public void Put([NotNull] [FromBody] string word)
        {
            if (word == null)
                throw new ArgumentNullException(nameof(word));

            try
            {
                _wordsProcessor.ProcessNewWord(word);
            }
            catch (LocalizableException ex)
            {
                _messenger.Send(ex.LocalizedMessage, MessengerTokens.UserWarningToken);
            }
        }
    }
}