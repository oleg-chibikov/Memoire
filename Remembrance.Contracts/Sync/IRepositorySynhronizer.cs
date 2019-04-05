namespace Remembrance.Contracts.Sync
{
    public interface IRepositorySynhronizer
    {
        string FileName { get; }

        void SyncRepository(string filePath);
    }
}