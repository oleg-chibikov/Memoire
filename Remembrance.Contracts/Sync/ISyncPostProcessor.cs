using JetBrains.Annotations;

namespace Remembrance.Contracts.Sync
{
    public interface ISyncPostProcessor<in T>
    {
        void OnEntityChanged([CanBeNull] T oldValue, [NotNull] T newValue);
    }
}