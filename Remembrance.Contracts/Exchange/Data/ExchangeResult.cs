using System.Collections.Generic;
using JetBrains.Annotations;

namespace Remembrance.Contracts.Exchange.Data
{
    public sealed class ExchangeResult
    {
        public ExchangeResult(bool success, [CanBeNull] IReadOnlyCollection<string> errors, int count)
        {
            Success = success;
            Errors = errors;
            Count = count;
        }

        public int Count { get; }

        [CanBeNull]
        public IReadOnlyCollection<string> Errors { get; }

        public bool Success { get; }
    }
}