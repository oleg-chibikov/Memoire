using System;
using System.Web.Http;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement;
using Scar.Common.Exceptions;
using Scar.Common.Messages;

namespace Remembrance.WebApi.Controllers
{
    [UsedImplicitly]
    public sealed class WordsController : ApiController
    {
        [NotNull]
        private readonly IMessageHub _messenger;

        [NotNull]
        private readonly IWordsProcessor _wordsProcessor;

        public WordsController([NotNull] IWordsProcessor wordsProcessor, [NotNull] IMessageHub messenger)
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
                _wordsProcessor.AddOrChangeWord(word);
            }
            catch (LocalizableException ex)
            {
                _messenger.Publish(ex.ToMessage());
            }
        }
    }
}