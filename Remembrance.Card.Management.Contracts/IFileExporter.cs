using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts.Data;

namespace Remembrance.Card.Management.Contracts
{
    public interface IFileExporter
    {
        [ItemNotNull]
        Task<ExchangeResult> ExportAsync([NotNull] string fileName, CancellationToken token);
    }
}