using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;

namespace Remembrance.Contracts.CardManagement
{
    public interface IFileExporter
    {
        [ItemNotNull]
        Task<ExchangeResult> ExportAsync([NotNull] string fileName, CancellationToken cancellationToken);
    }
}