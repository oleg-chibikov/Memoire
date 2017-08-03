using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Scar.Common.Events;

namespace Remembrance.Contracts.CardManagement
{
    public interface ICardsExchanger
    {
        [NotNull]
        Task ExportAsync(CancellationToken token);

        [NotNull]
        Task ImportAsync(CancellationToken token);

        event EventHandler<ProgressEventArgs> Progress;
    }
}