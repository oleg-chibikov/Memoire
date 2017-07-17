using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Card.Management.Contracts.Data;
using Scar.Common.Events;

namespace Remembrance.Card.Management.Contracts
{
    public interface IFileImporter
    {
        [ItemNotNull]
        Task<ExchangeResult> ImportAsync([NotNull] string fileName, CancellationToken token);

        event EventHandler<ProgressEventArgs> Progress;
    }
}