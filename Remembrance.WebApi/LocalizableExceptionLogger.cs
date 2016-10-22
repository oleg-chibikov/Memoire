using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Common.Logging;
using Scar.Common.Exceptions;

namespace Remembrance.WebApi
{
    public class LocalizableExceptionLogger : IExceptionLogger
    {
        public Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
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