using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Common.Logging;
using JetBrains.Annotations;
using Scar.Common.Exceptions;

namespace Remembrance.WebApi
{
    //TODO: WebApi Library
    public sealed class LocalizableExceptionLogger : IExceptionLogger
    {
        public Task LogAsync([NotNull] ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            var logger = (ILog)context.Request.GetDependencyScope().GetService(typeof(ILog));
            var localizableException = context.Exception as LocalizableException;
            if (localizableException != null)
                logger.Warn(localizableException.Message);
            else
                logger.Error(context.Exception);
            return Task.FromResult(0);
        }
    }
}