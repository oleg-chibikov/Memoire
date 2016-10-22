using JetBrains.Annotations;

namespace Remembrance.Card.Management.Contracts
{
    public interface IFileImporter
    {
        bool Import([NotNull] string fileName, [CanBeNull] out string[] errors, out int count);
    }
}