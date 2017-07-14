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
        private readonly IWordsProcessor _wordsProcessor;

        public WordsController([NotNull] IWordsProcessor wordsProcessor)
        {
            _wordsProcessor = wordsProcessor ?? throw new ArgumentNullException(nameof(wordsProcessor));
        }

        [HttpPut]
        [UsedImplicitly]
        public void Put([NotNull] [FromBody] string word)
        {
            if (word == null)
                throw new ArgumentNullException(nameof(word));

            _wordsProcessor.ProcessNewWord(word);
        }
    }
}