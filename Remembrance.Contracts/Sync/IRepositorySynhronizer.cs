using JetBrains.Annotations;

namespace Remembrance.Contracts.Sync
{
    public interface IRepositorySynhronizer
    {
        string FileName { get; }

        void SyncRepository([NotNull] string filePath);
    }
}