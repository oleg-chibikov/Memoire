using System;
using System.Threading;
using System.Threading.Tasks;
using Mémoire.Contracts.Processing;
using Mémoire.Contracts.Processing.Data;
using Microsoft.AspNetCore.Mvc;

namespace Mémoire.WebApi.Controllers
{
    [Route("api/words")]
    [ApiController]
    public sealed class WordsController : ControllerBase
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

        [HttpPut("{word}")]
        public async Task PutFromUriAsync(string word)
        {
            _ = word ?? throw new ArgumentNullException(nameof(word));

            await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(word), CancellationToken.None).ConfigureAwait(false);
        }
    }
}
