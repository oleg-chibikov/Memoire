using System.Threading.Tasks;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncPreProcessor<in T>
    {
        Task<bool> BeforeEntityChangedAsync(T oldValue, T newValue);
    }
}