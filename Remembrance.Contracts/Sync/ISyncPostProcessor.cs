using System.Threading.Tasks;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncPostProcessor<in T>
    {
        Task AfterEntityChangedAsync(T oldValue, T newValue);
    }
}