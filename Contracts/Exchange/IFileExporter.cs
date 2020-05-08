using System;
using Remembrance.Contracts.Exchange.Data;
using Scar.Common.Events;

namespace Remembrance.Contracts.Exchange
{
    public interface IFileExporter
    {
        event EventHandler<ProgressEventArgs> Progress;

        ExchangeResult Export(string fileName);
    }
}
