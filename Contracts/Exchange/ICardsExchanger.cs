using System;
using System.Threading;
using System.Threading.Tasks;
using Scar.Common.Events;

namespace Mémoire.Contracts.Exchange
{
    public interface ICardsExchanger
    {
        event EventHandler<ProgressEventArgs> Progress;

        Task ExportAsync(CancellationToken cancellationToken);

        Task ImportAsync(CancellationToken cancellationToken);
    }
}
