using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Scar.Common.Events;

namespace Remembrance.Contracts.Exchange
{
    public interface ICardsExchanger
    {
        [NotNull]
        Task ExportAsync(CancellationToken cancellationToken);

        [NotNull]
        Task ImportAsync(CancellationToken cancellationToken);

        event EventHandler<ProgressEventArgs> Progress;
    }
}