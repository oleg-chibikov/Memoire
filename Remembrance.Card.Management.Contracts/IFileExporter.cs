using JetBrains.Annotations;

namespace Remembrance.Card.Management.Contracts
{
    public interface IFileExporter
    {
        bool Export([NotNull] string fileName);
    }
}