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
        private readonly IWordsAdder wordsAdder;

        public WordsController([NotNull] IWordsAdder wordsAdder)
        {
            if (wordsAdder == null)
                throw new ArgumentNullException(nameof(wordsAdder));
            this.wordsAdder = wordsAdder;
        }

        [HttpPut]
        public void Put([FromBody] string word)
        {
            wordsAdder.AddWord(word);
        }
    }
}