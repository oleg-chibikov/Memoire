﻿using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Remembrance.Resources;
using Scar.Common.Exceptions;

namespace Remembrance.WebApi
{
    internal class LocalizableExceptionHandler : IExceptionHandler
    {
        public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            var localizableException = context.Exception as LocalizableException;
            var message = localizableException?.LocalizedMessage ?? Errors.DefaultError;
            context.Result = new TextPlainErrorResult
            {
                Request = context.ExceptionContext.Request,
                Content = message
            };
            return Task.FromResult(0);
        }

        private class TextPlainErrorResult : IHttpActionResult
        {
            public HttpRequestMessage Request { private get; set; }

            public string Content { private get; set; }

            public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent(Content),
                    RequestMessage = Request
                };
                return await Task.FromResult(response);
            }
        }
    }
}