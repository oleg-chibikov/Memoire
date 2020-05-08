using System.Collections.Generic;

namespace Mémoire.Contracts.Exchange.Data
{
    public sealed class ExchangeResult
    {
        public ExchangeResult(bool success, IReadOnlyCollection<string>? errors, int count)
        {
            Success = success;
            Errors = errors;
            Count = count;
        }

        public int Count { get; }

        public IReadOnlyCollection<string>? Errors { get; }

        public bool Success { get; }
    }
}
