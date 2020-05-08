using System.Threading.Tasks;

namespace Mémoire.Contracts.Sync
{
    public interface ISyncPostProcessor<in T>
    {
        Task AfterEntityChangedAsync(T oldValue, T newValue);
    }
}
