using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;

namespace Remembrance.WebApi.Controllers
{
    [UsedImplicitly]
    public sealed class WordsController : ApiController
    {
        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        public WordsController([NotNull] ITranslationEntryProcessor translationEntryProcessor)
        {
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        [HttpPut]
        [UsedImplicitly]
        [NotNull]
        public async Task PutAsync([NotNull] [FromBody] string word)
        {
            _ = word ?? throw new ArgumentNullException(nameof(word));
            await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(word), CancellationToken.None).ConfigureAwait(false);
        }
    }
}