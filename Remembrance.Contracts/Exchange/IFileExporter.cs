using System;
using System.Threading;
using System.Threading.Tasks;
using Remembrance.Contracts.Exchange.Data;
using Scar.Common.Events;

namespace Remembrance.Contracts.Exchange
{
    public interface IFileExporter
    {
        event EventHandler<ProgressEventArgs> Progress;

        Task<ExchangeResult> ExportAsync(string fileName, CancellationToken cancellationToken);
    }
}