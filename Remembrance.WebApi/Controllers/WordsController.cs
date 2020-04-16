using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.WebApi.Controllers
{
    public sealed class WordsController : ApiController
    {
        readonly ITranslationEntryProcessor _translationEntryProcessor;

        public WordsController(ITranslationEntryProcessor translationEntryProcessor)
        {
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        [HttpPut]
        public async Task PutAsync([FromBody] string word)
        {
            _ = word ?? throw new ArgumentNullException(nameof(word));
            await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(word), CancellationToken.None).ConfigureAwait(false);
        }
    }
}
