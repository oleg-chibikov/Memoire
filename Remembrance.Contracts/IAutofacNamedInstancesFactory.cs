using Autofac.Core;
using JetBrains.Annotations;

namespace Remembrance.Contracts
{
    public interface IAutofacNamedInstancesFactory
    {
        [NotNull]
        T GetInstance<T>(params Parameter[] parameters);
    }
}