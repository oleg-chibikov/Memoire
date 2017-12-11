using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Remembrance.Contracts.CardManagement.Data;
using Scar.Common.Events;

namespace Remembrance.Contracts.CardManagement
{
    public interface IFileImporter
    {
        [ItemNotNull]
        Task<ExchangeResult> ImportAsync([NotNull] string fileName, CancellationToken cancellationToken);

        event EventHandler<ProgressEventArgs> Progress;
    }
}