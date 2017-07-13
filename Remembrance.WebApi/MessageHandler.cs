using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.WebApi
{
    //TODO: WebApi Library
    internal abstract class MessageHandler : DelegatingHandler
    {
        protected abstract Task IncomingMessageAsync([NotNull] HttpRequestMessage request, [NotNull] string requestInfo, [CanBeNull] string message);
        protected abstract Task OutgoingMessageAsync([NotNull] HttpRequestMessage request, [NotNull] string requestInfo, [CanBeNull] string message);

        [ItemNotNull]
        protected override async Task<HttpResponseMessage> SendAsync([NotNull] HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            var requestInfo = $"{request.Method} {request.RequestUri}";
            var requestMessage = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync();
            await IncomingMessageAsync(request, requestInfo, requestMessage);
            var response = await base.SendAsync(request, cancellationToken);
            string responseMessage = null;
            if (response.Content != null)
                responseMessage = await response.Content.ReadAsStringAsync();
            await OutgoingMessageAsync(request, requestInfo, responseMessage);
            return response;
        }
    }
}