using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Exchange.Data;
using Scar.Common.Events;

namespace Remembrance.Contracts.Exchange
{
    public interface IFileExporter
    {
        event EventHandler<ProgressEventArgs> Progress;

        [ItemNotNull]
        [NotNull]
        Task<ExchangeResult> ExportAsync([NotNull] string fileName, CancellationToken cancellationToken);
    }
}