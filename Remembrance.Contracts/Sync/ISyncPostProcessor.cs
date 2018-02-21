using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncPostProcessor<in T>
    {
        Task OnEntityChangedAsync([CanBeNull] T oldValue, [NotNull] T newValue);
    }
}