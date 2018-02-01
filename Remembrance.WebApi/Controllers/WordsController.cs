using System;
using System.Threading;
using System.Web.Http;
using Easy.MessageHub;
using JetBrains.Annotations;
using Remembrance.Contracts;
using Remembrance.Contracts.DAL.Model;
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
        private readonly ITranslationEntryProcessor _translationEntryProcessor;

        public WordsController([NotNull] ITranslationEntryProcessor translationEntryProcessor, [NotNull] IMessageHub messenger)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        [HttpPut]
        [UsedImplicitly]
        public async void PutAsync([NotNull] [FromBody] string word)
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
                _messenger.Publish(ex.ToMessage());
            }
        }
    }
}