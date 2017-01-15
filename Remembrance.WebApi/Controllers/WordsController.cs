using System;
using System.Web.Http;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts;

namespace Remembrance.WebApi.Controllers
{
    [UsedImplicitly]
    public sealed class WordsController : ApiController
    {
        [NotNull]
        private readonly IWordsProcessor wordsProcessor;

        public WordsController([NotNull] IWordsProcessor wordsProcessor)
        {
            if (wordsProcessor == null)
                throw new ArgumentNullException(nameof(wordsProcessor));
            this.wordsProcessor = wordsProcessor;
        }

        [HttpPut, UsedImplicitly]
        public void Put([FromBody] string word)
        {
            wordsProcessor.ProcessNewWord(word);
        }
    }
}