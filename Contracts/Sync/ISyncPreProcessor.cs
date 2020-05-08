using System.Threading.Tasks;

namespace Mémoire.Contracts.Sync
{
    public interface ISyncPreProcessor<in T>
    {
        Task<bool> BeforeEntityChangedAsync(T oldValue, T newValue);
    }
}
