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
        private readonly IDisposable apiHost;
        private readonly ILifetimeScope innerScope;

        [NotNull]
        private readonly ILog logger;

        public ApiHoster([NotNull] ILog logger, [NotNull] ILifetimeScope lifetimeScope)
        {
            if (lifetimeScope == null)
                throw new ArgumentNullException(nameof(lifetimeScope));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            logger.Debug("Starting WebApi...");
            innerScope = lifetimeScope.BeginLifetimeScope(innerBuilder => innerBuilder.RegisterApiControllers(Assembly.GetExecutingAssembly()).InstancePerDependency());
            apiHost = WebApp.Start(
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

                    config.DependencyResolver = new AutofacWebApiDependencyResolver(innerScope);

                    app.UseAutofacMiddleware(innerScope);
                    app.UseAutofacWebApi(config);
                    app.DisposeScopeOnAppDisposing(innerScope);
                    app.UseWebApi(config);
                });
            logger.Debug("WebApi is started");
        }

        public void Dispose()
        {
            apiHost.Dispose();
            innerScope.Dispose();
            logger.Debug("WebApi has been stopped...");
        }
    }
}