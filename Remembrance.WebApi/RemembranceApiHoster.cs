using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Remembrance.Contracts.Processing;
using Scar.Common.WebApi;

namespace Remembrance.WebApi
{
    public class RemembranceApiHoster : ApiHoster
    {
        readonly ITranslationEntryProcessor _translationEntryProcessor;

        public RemembranceApiHoster(ITranslationEntryProcessor translationEntryProcessor)
        {
            _translationEntryProcessor = translationEntryProcessor ?? throw new ArgumentNullException(nameof(translationEntryProcessor));
        }

        public override Uri BaseUrl { get; } = new Uri("http://localhost:2053");

        protected override Assembly WebApiAssembly { get; } = Assembly.GetExecutingAssembly();

        protected override void RegisterDependencies(IServiceCollection x)
        {
            x.AddSingleton(_translationEntryProcessor);
        }
    }
}
