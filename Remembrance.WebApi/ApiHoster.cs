using System.Reflection;
using System.Web.Http;
using Autofac;
using Common.Logging;
using JetBrains.Annotations;
using Scar.Common.WebApi;

namespace Remembrance.WebApi
{
    [UsedImplicitly]
    public sealed class ApiHoster : AutofacApiHoster
    {
        public ApiHoster(ILog logger, ILifetimeScope lifetimeScope)
            : base(logger, lifetimeScope)
        {
        }

        [NotNull]
        protected override string BaseAddress => "http://localhost:2053/";

        [NotNull]
        protected override Assembly ControllersAssembly => Assembly.GetExecutingAssembly();

        protected override void RegisterRoutes(HttpRouteCollection routes)
        {
            routes.MapHttpRoute(
                "DefaultApi",
                "api/{controller}/{word}",
                new
                {
                    word = RouteParameter.Optional
                });
        }
    }
}