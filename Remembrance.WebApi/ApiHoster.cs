using System;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Autofac;
using Autofac.Integration.WebApi;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Owin.Hosting;
using Owin;

namespace Remembrance.WebApi
{
    [UsedImplicitly]
    public sealed class ApiHoster : IDisposable
    {
        private const string BaseAddress = "http://localhost:2053/";

        private readonly IDisposable _apiHost;

        private readonly ILifetimeScope _innerScope;

        [NotNull]
        private readonly ILog _logger;

        public ApiHoster([NotNull] ILog logger, [NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.Trace("Starting WebApi...");
            _innerScope = lifetimeScope.BeginLifetimeScope(innerBuilder => innerBuilder.RegisterApiControllers(Assembly.GetExecutingAssembly()).InstancePerDependency());
            _apiHost = WebApp.Start(
                new StartOptions(BaseAddress),
                app =>
                {
                    var config = new HttpConfiguration();
                    config.Services.Replace(typeof(IExceptionHandler), new LocalizableExceptionHandler());
                    config.Services.Replace(typeof(IExceptionLogger), new LocalizableExceptionLogger());

                    config.MessageHandlers.Add(new MessageLoggingHandler());
                    config.Routes.MapHttpRoute(
                        "DefaultApi",
                        "api/{controller}/{word}",
                        new
                        {
                            word = RouteParameter.Optional
                        });

                    config.DependencyResolver = new AutofacWebApiDependencyResolver(_innerScope);

                    app.UseAutofacMiddleware(_innerScope);
                    app.UseAutofacWebApi(config);
                    app.DisposeScopeOnAppDisposing(_innerScope);
                    app.UseWebApi(config);
                });
            logger.Trace("WebApi is started");
        }

        public void Dispose()
        {
            _apiHost.Dispose();
            _innerScope.Dispose();
            _logger.Trace("WebApi has been stopped...");
        }
    }
}