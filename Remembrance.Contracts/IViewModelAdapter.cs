using JetBrains.Annotations;

namespace Remembrance.Contracts
{
    public interface IViewModelAdapter
    {
        [NotNull]
        TDestination Adapt<TDestination>([NotNull] object source);

        [NotNull]
        TDestination Adapt<TSource, TDestination>([NotNull] TSource source, [NotNull] TDestination destination);
    }
}