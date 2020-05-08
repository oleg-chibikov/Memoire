using System;
using Mémoire.Contracts.Exchange.Data;
using Scar.Common.Events;

namespace Mémoire.Contracts.Exchange
{
    public interface IFileExporter
    {
        event EventHandler<ProgressEventArgs> Progress;

        ExchangeResult Export(string fileName);
    }
}
