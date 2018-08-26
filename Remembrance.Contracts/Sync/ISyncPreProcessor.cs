using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncPreProcessor<in T>
    {
        [NotNull]
        Task<bool> BeforeEntityChangedAsync([CanBeNull] T oldValue, [NotNull] T newValue);
    }
}