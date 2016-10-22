namespace Remembrance.TypeAdapter.Contracts
{
    public interface IViewModelAdapter
    {
        TDestination Adapt<TDestination>(object source);
        TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination);
    }
}