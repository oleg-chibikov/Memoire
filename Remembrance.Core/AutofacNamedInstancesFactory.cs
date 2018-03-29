using System;
using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Remembrance.Contracts;

namespace Remembrance.Core
{
    [UsedImplicitly]
    public sealed class AutofacNamedInstancesFactory : IAutofacNamedInstancesFactory
    {
        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;

        public AutofacNamedInstancesFactory([NotNull] ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope ?? throw new ArgumentNullException(nameof(lifetimeScope));
        }

        public T GetInstance<T>(params Parameter[] parameters)
        {
            return _lifetimeScope.ResolveNamed<T>(typeof(T).FullName, parameters);
        }
    }
}