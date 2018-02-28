using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.Exchange.Data;
using Scar.Common.Events;

namespace Remembrance.Contracts.Exchange
{
    public interface IFileImporter
    {
        event EventHandler<ProgressEventArgs> Progress;

        [ItemNotNull]
        Task<ExchangeResult> ImportAsync([NotNull] string fileName, CancellationToken cancellationToken);
    }
}