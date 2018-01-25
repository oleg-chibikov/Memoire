using Autofac.Core;
using JetBrains.Annotations;

namespace Remembrance.Contracts.DAL
{
    public interface INamedInstancesFactory
    {
        [NotNull]
        T GetInstance<T>(params Parameter[] parameters);
    }
}