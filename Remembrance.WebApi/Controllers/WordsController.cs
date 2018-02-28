using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts.Processing;
using Remembrance.Contracts.Processing.Data;
using Scar.Common.Exceptions;
using Scar.Common.Messages;

namespace Remembrance.WebApi.Controllers
{
    [UsedImplicitly]
    public sealed class WordsController : ApiController
    {
        [NotNull]
        private readonly IMessageHub _messageHub;

        [NotNull]
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        public WordsController([NotNull] ITranslationEntryProcessor translationEntryProcessor, [NotNull] IMessageHub messageHub)
        {
            _messageHub = messageHub ?? throw new ArgumentNullException(nameof(messageHub));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        [HttpPut]
        [UsedImplicitly]
        [NotNull]
        public async Task PutAsync([NotNull] [FromBody] string word)
        {
            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            try
            {
                await _translationEntryProcessor.AddOrUpdateTranslationEntryAsync(new TranslationEntryAdditionInfo(word), CancellationToken.None).ConfigureAwait(false);
            }
            catch (LocalizableException ex)
            {
                _messageHub.Publish(ex.ToMessage());
            }
        }
    }
}